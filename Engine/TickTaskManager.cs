namespace Engine
{
    public static class TickTaskManager
    {
        public static bool EnableDebug = false;
        private static List<TickTask> _tasks = new List<TickTask>();
        private static readonly System.Collections.Concurrent.ConcurrentQueue<TickTask> _added = new System.Collections.Concurrent.ConcurrentQueue<TickTask>();

        public static void AddTask(TickTask task)
        {
            _added.Enqueue(task);

            if (EnableDebug)
            {
                Debug.Log($"[TickTaskManager] Task added. Tasks running: " + (_tasks.Count + _added.Count));
            }
        }

        public static void UpdateTasks()
        {
            while (_added.TryDequeue(out var added)) _tasks.Add(added);

            if (_tasks.Count == 0) return;

            for (int i = _tasks.Count - 1; i >= 0; i--)
            {
                var task = _tasks[i];

                if (task == null)
                {
                    _tasks.RemoveAt(i);
                    if (EnableDebug)
                    {
                        Debug.Log($"[TickTaskManager] Task was null and removed. Tasks running: " + _tasks.Count);
                    }
                    continue;
                }

                if (task.IsStopped)
                {
                    _tasks.RemoveAt(i);
                    if (EnableDebug)
                    {
                        Debug.Log($"[TickTaskManager] Task removed. Tasks running: " + _tasks.Count);
                    }
                    continue;
                }

                task.Update();

                if (task.IsComplete)
                {
                    _tasks.RemoveAt(i);
                    if (EnableDebug)
                    {
                        Debug.Log($"[TickTaskManager] Task removed. Tasks running: " + _tasks.Count);
                    }
                }
            }
        }

        public static void Dispose()
        {

            _tasks.Clear();
            _added.Clear();
        }


    }

}
