using Engine.Multithreading;
using OpenTK.Mathematics;

namespace Engine.GUI
{
    public class ThreadsElement : OverlayElement
    {
        public static readonly Color4 White = new Color4(1f, 1f, 1f, 1f);
        public static readonly Color4 Red = new Color4(1f, 0.3f, 0.3f, 1f);
        public static readonly Color4 Yellow = new Color4(1f, 1f, 0.3f, 1f);
        public static readonly Color4 Green = new Color4(0.35f, 1f, 0.35f, 1f);
        public static readonly Color4 Orange = new Color4(1f, 0.6f, 0.2f, 1f);

        private float timer = 1f;
        private long cachedQueued = 0;
        private long cachedCompleted = 0;
        private long cachedFailed = 0;
        private int cachedPending = 0;
        private int cachedActive = 0;

        public override void Write(OverlayText text)
        {
            timer += text.Elapsed;
            if (timer >= 1f)
            {
                cachedActive = WorkerPoolManager.ActiveThreads;
                cachedQueued = WorkerPoolManager.TasksQueued;
                cachedCompleted = WorkerPoolManager.TasksCompleted;
                cachedFailed = WorkerPoolManager.TasksFailed;
                cachedPending = WorkerPoolManager.PendingTasks;
                timer = 0f;
            }

            text.Section("[THREADS]");
            var colActive = cachedActive == 0 ? White : Green;
            var colQueued = cachedQueued == 0 ? White : Yellow;
            var colCompleted = cachedCompleted == 0 ? White : Green;
            var colFailed = cachedFailed == 0 ? White : Red;
            var colPending = cachedPending == 0 ? White : Orange;

            text.Line($"Active workers: {cachedActive}", colActive);
            text.Line($"Queued: {cachedQueued}", colQueued);
            text.Append($"| Completed: {cachedCompleted}", colCompleted);

            text.Line($"Pending: {cachedPending}", colPending);
            text.Append($"| Failed: {cachedFailed}", colFailed);

        }
    }
}
