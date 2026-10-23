using System.Collections.Concurrent;


namespace Engine.Multithreading
{
    public static class WorkerPoolManager
    {
        public enum Priority { High, Medium, Low }

        static readonly ConcurrentQueue<WorkItem> highQueue = new ConcurrentQueue<WorkItem>();
        static readonly ConcurrentQueue<WorkItem> medQueue = new ConcurrentQueue<WorkItem>();
        static readonly ConcurrentQueue<WorkItem> lowQueue = new ConcurrentQueue<WorkItem>();
        static readonly SemaphoreSlim generalAvailable = new SemaphoreSlim(0);
        static volatile bool generalAddingCompleted;
    
        static readonly ConcurrentDictionary<string, BlockingCollection<WorkItem>> namedQueues = new ConcurrentDictionary<string, BlockingCollection<WorkItem>>();
        static readonly List<Thread> workers;
        static readonly ConcurrentDictionary<string, Thread> dedicatedWorkers = new ConcurrentDictionary<string, Thread>();
        static readonly CancellationTokenSource cts = new CancellationTokenSource();

        static long namedPendingCount = 0;

        static long tasksQueued = 0;
        static long tasksCompleted = 0;
        static long tasksFailed = 0;

        static WorkerPoolManager()
        {
            int count = Math.Max(1, Environment.ProcessorCount - 1 );
            workers = new List<Thread>(count);
            for (int i = 0; i < count; i++)
            {
                var thread = new Thread(GeneralWorkerLoop) { IsBackground = true, Name = $"WorkerPool-{i}" };
                workers.Add(thread);
                thread.Start();
            }
        }

        public static long TasksQueued => Interlocked.Read(ref tasksQueued);
        public static long TasksCompleted => Interlocked.Read(ref tasksCompleted);
        public static long TasksFailed => Interlocked.Read(ref tasksFailed);
        public static int ActiveThreads => workers.Count + dedicatedWorkers.Count;
        public static int PendingTasks => highQueue.Count + medQueue.Count + lowQueue.Count + GetNamedPending();

        public static Task Enqueue(Action<CancellationToken> action, Priority priority, CancellationToken token = default)
        {
            
            Interlocked.Increment(ref tasksQueued);
            var tcs = new TaskCompletionSource<bool>();
            var item = new WorkItem
            {
                Execute = () =>
                {
                    if (token.IsCancellationRequested)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    try
                    {
                        action(token);
                        Interlocked.Increment(ref tasksCompleted);
                        tcs.SetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        tcs.SetCanceled();
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref tasksFailed);
                        Debug.Error($"[WorkerPool] Task failed: {ex}");
                        tcs.SetException(ex);
                    }
                }
            };
            if (!TryAddGeneral(QueueFor(priority), item)) tcs.TrySetCanceled();
            return tcs.Task;
        }

        public static Task<T> Enqueue<T>(Func<CancellationToken, T> func, Priority priority, CancellationToken token = default)
        {
         
            Interlocked.Increment(ref tasksQueued);
            var tcs = new TaskCompletionSource<T>();
            var item = new WorkItem
            {
                Execute = () =>
                {
                    if (token.IsCancellationRequested)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    try
                    {
                        var result = func(token);
                        Interlocked.Increment(ref tasksCompleted);
                        tcs.SetResult(result);
                    }
                    catch (OperationCanceledException)
                    {
                        tcs.SetCanceled();
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref tasksFailed);
                        Debug.Error($"[WorkerPool] Task failed: {ex}");
                        tcs.SetException(ex);
                    }
                }
            };
            if (!TryAddGeneral(QueueFor(priority), item)) tcs.TrySetCanceled();
            return tcs.Task;
        }

        public static void AddDedicatedWorker(string name)
        {
            var queue = new BlockingCollection<WorkItem>();
            if (namedQueues.TryAdd(name, queue))
            {
                var thread = new Thread(() => DedicatedWorkerLoop(name)) { IsBackground = true, Name = name };
                dedicatedWorkers[name] = thread;
                thread.Start();
            }
        }

        public static Task EnqueueDedicated(string name, Action<CancellationToken> action, CancellationToken token = default)
        {
            if (!namedQueues.TryGetValue(name, out var queue)) throw new ArgumentException("No such dedicated worker");

            Interlocked.Increment(ref tasksQueued);
            Interlocked.Increment(ref namedPendingCount);

            var tcs = new TaskCompletionSource<bool>();
            var item = new WorkItem
            {
                Execute = () =>
                {
                    if (token.IsCancellationRequested)
                    {
                        tcs.SetCanceled(); return;
                    }
                    try
                    {
                        action(token);
                        Interlocked.Increment(ref tasksCompleted);
                        tcs.SetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        tcs.SetCanceled();
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref tasksFailed);
                        tcs.SetException(new Exception("Dedicated task failed"));
                    }
                }
            };
            if (!TryAdd(queue, item))
            {
                Interlocked.Decrement(ref namedPendingCount);
                tcs.TrySetCanceled();
            }
            return tcs.Task;
        }

        static ConcurrentQueue<WorkItem> QueueFor(Priority priority) => priority switch
        {
            Priority.High => highQueue,
            Priority.Medium => medQueue,
            _ => lowQueue
        };

        static bool TryAddGeneral(ConcurrentQueue<WorkItem> queue, WorkItem item)
        {
            if (generalAddingCompleted) return false;
            queue.Enqueue(item);
            generalAvailable.Release();
            return true;
        }

        static bool TryTakeGeneral(out WorkItem item)
        {
            return highQueue.TryDequeue(out item) || medQueue.TryDequeue(out item) || lowQueue.TryDequeue(out item);
        }

        static bool TryAdd(BlockingCollection<WorkItem> queue, WorkItem item)
        {
            if (queue.IsAddingCompleted) return false;
            try
            {
                queue.Add(item);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static void Shutdown()
        {
            generalAddingCompleted = true;
            cts.Cancel();
            foreach (var q in namedQueues.Values) q.CompleteAdding();
            foreach (var t in workers) t.Join();
            foreach (var t in dedicatedWorkers.Values) t.Join();
        }

        static void GeneralWorkerLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    generalAvailable.Wait(cts.Token);
                    if (TryTakeGeneral(out var item)) item.Execute();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }


        static void DedicatedWorkerLoop(string name)
        {
            var queue = namedQueues[name];
            foreach (var item in queue.GetConsumingEnumerable(cts.Token))
            {
                Interlocked.Decrement(ref namedPendingCount);
                item.Execute();
            }
        }

        static int GetNamedPending()
        {
            return (int)Interlocked.Read(ref namedPendingCount);
        }

        class WorkItem { public Action Execute; }
    }
}