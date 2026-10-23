using System.Collections.Concurrent;


namespace Engine.Multithreading
{
    public static class WorkerPoolManager
    {
        public enum Priority { High, Medium, Low }

        static readonly ConcurrentQueue<IWorkItem> highQueue = new ConcurrentQueue<IWorkItem>();
        static readonly ConcurrentQueue<IWorkItem> medQueue = new ConcurrentQueue<IWorkItem>();
        static readonly ConcurrentQueue<IWorkItem> lowQueue = new ConcurrentQueue<IWorkItem>();
        static readonly SemaphoreSlim available = new SemaphoreSlim(0);
        static readonly Thread[] workers;
        static readonly CancellationTokenSource cts = new CancellationTokenSource();
        static volatile bool addingCompleted;

        static long pendingCount = 0;
        static long tasksQueued = 0;
        static long tasksCompleted = 0;
        static long tasksFailed = 0;

        static WorkerPoolManager()
        {
            int count = Math.Max(1, Environment.ProcessorCount - 1);
            workers = new Thread[count];
            for (int i = 0; i < count; i++)
            {
                var thread = new Thread(WorkerLoop) { IsBackground = true, Name = $"WorkerPool-{i}", Priority = ThreadPriority.BelowNormal };
                workers[i] = thread;
                thread.Start();
            }
        }

        public static long TasksQueued => Interlocked.Read(ref tasksQueued);
        public static long TasksCompleted => Interlocked.Read(ref tasksCompleted);
        public static long TasksFailed => Interlocked.Read(ref tasksFailed);
        public static int ActiveThreads => workers.Length;
        public static int PendingTasks => (int)Interlocked.Read(ref pendingCount);

        public static Task Enqueue(Action<CancellationToken> action, Priority priority, CancellationToken token = default)
        {
            return Schedule(new ActionWorkItem(action, token), priority, token);
        }

        public static Task<T> Enqueue<T>(Func<CancellationToken, T> func, Priority priority, CancellationToken token = default)
        {
            return Schedule(new FuncWorkItem<T>(func, token), priority, token);
        }

        internal static void ReportCompleted() => Interlocked.Increment(ref tasksCompleted);

        internal static void ReportFailed(Exception ex)
        {
            Interlocked.Increment(ref tasksFailed);
            Debug.Error($"[WorkerPool] Task failed: {ex}");
        }

        public static void Shutdown()
        {
            addingCompleted = true;
            cts.Cancel();
            foreach (var t in workers) t.Join();
            while (TryTake(out var item)) item.Cancel();
        }

        static Task<T> Schedule<T>(WorkItem<T> item, Priority priority, CancellationToken token)
        {
            Interlocked.Increment(ref tasksQueued);
            if (addingCompleted || token.IsCancellationRequested)
            {
                item.Cancel();
                return item.Task;
            }

            Interlocked.Increment(ref pendingCount);
            QueueFor(priority).Enqueue(item);
            available.Release();
            return item.Task;
        }

        static ConcurrentQueue<IWorkItem> QueueFor(Priority priority) => priority switch
        {
            Priority.High => highQueue,
            Priority.Medium => medQueue,
            _ => lowQueue
        };

        static bool TryTake(out IWorkItem item)
        {
            if (highQueue.TryDequeue(out item) || medQueue.TryDequeue(out item) || lowQueue.TryDequeue(out item))
            {
                Interlocked.Decrement(ref pendingCount);
                return true;
            }
            return false;
        }

        static void WorkerLoop()
        {
            while (true)
            {
                try
                {
                    available.Wait(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (TryTake(out var item)) item.Execute();
            }
        }
    }
}
