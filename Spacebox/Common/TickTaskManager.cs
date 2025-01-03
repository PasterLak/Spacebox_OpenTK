

namespace Spacebox.Common
{
    public static class TickTaskManager
    {
        private static List<TickTask> _tasks = new List<TickTask>();

        public static void AddTask(TickTask task)
        {
            _tasks.Add(task);
        }

        public static void UpdateTasks()
        {
            if(_tasks.Count == 0) return;

            for (int i = _tasks.Count - 1; i >= 0; i--)
            {
                var task = _tasks[i];

                if(task == null)
                {
                    _tasks.RemoveAt(i);
                    continue;
                }

                task.Update();

                if (task.IsComplete)
                {
                    _tasks.RemoveAt(i);
                }
            }
        }

        public static void Dispose()
        {

            _tasks.Clear();
        }

      
    }

}
