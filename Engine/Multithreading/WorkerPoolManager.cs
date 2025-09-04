using System.Collections.Concurrent;


namespace Engine.Multithreading
{
    public static class WorkerPoolManager
    {
        public enum Priority { High, Medium, Low }

        static readonly BlockingCollection<WorkItem> highQueue = new BlockingCollection<WorkItem>();
        static readonly BlockingCollection<WorkItem> medQueue = new BlockingCollection<WorkItem>();
        static readonly BlockingCollection<WorkItem> lowQueue = new BlockingCollection<WorkItem>();
    
        static readonly ConcurrentDictionary<string, BlockingCollection<WorkItem>> namedQueues = new ConcurrentDictionary<string, BlockingCollection<WorkItem>>();
        static readonly List<Thread> workers;
        static readonly ConcurrentDictionary<string, Thread> dedicatedWorkers = new ConcurrentDictionary<string, Thread>();
        static readonly CancellationTokenSource cts = new CancellationTokenSource();

        static long tasksQueued = 0;
        static long tasksCompleted = 0;
        static long tasksFailed = 0;

        const int MIN_DELAY_MS = 5;
        const int MAX_DELAY_MS = 50;

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
        public static int PendingTasks => highQueue.Count + lowQueue.Count + GetNamedPending();

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
                    catch (Exception)
                    {
                        Interlocked.Increment(ref tasksFailed);
                        tcs.SetException(new Exception("WorkerPool task failed"));
                    }
                }
            };
            switch (priority)
            {
                case Priority.High: highQueue.Add(item); break;
                case Priority.Medium: medQueue.Add(item); break;
                default: lowQueue.Add(item); break;
            }
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
                    catch (Exception)
                    {
                        Interlocked.Increment(ref tasksFailed);
                        tcs.SetException(new Exception("WorkerPool task failed"));
                    }
                }
            };
            switch (priority)
            {
                case Priority.High: highQueue.Add(item); break;
                case Priority.Medium: medQueue.Add(item); break;
                default: lowQueue.Add(item); break;
            }
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
            queue.Add(item);
            return tcs.Task;
        }

        public static void Shutdown()
        {
            cts.Cancel();
            highQueue.CompleteAdding();
            lowQueue.CompleteAdding();
            foreach (var q in namedQueues.Values) q.CompleteAdding();
            foreach (var t in workers) t.Join();
            foreach (var t in dedicatedWorkers.Values) t.Join();
        }

        static void GeneralWorkerLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                WorkItem item;
                int totalPending = highQueue.Count + medQueue.Count + lowQueue.Count;
                int workerCount = workers.Count;
                int delay = totalPending > workerCount ? MIN_DELAY_MS : MAX_DELAY_MS;
                // System.OperationCanceledException: 'The operation was canceled.'
                if (highQueue.TryTake(out item, highQueue.Count > workerCount ? 1 : MIN_DELAY_MS, cts.Token) ||
                    medQueue.TryTake(out item, delay, cts.Token) ||
                    lowQueue.TryTake(out item, delay, cts.Token))
                {
                    item.Execute();
                }
            }
        }


        static void DedicatedWorkerLoop(string name)
        {
            var queue = namedQueues[name];
            foreach (var item in queue.GetConsumingEnumerable(cts.Token))
                item.Execute();
        }

        static int GetNamedPending()
        {
            int sum = 0;
            foreach (var q in namedQueues.Values) sum += q.Count;
            return sum;
        }

        class WorkItem { public Action Execute; }
    }
}