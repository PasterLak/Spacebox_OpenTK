using Engine.Multithreading;
using ImGuiNET;
using NumVector4 = System.Numerics.Vector4;

namespace Engine.GUI
{
    public class ThreadsElement : OverlayElement
    {
        public static readonly NumVector4 White = new NumVector4(1f, 1f, 1f, 1f);
        public static readonly NumVector4 Red = new NumVector4(1f, 0f, 0f, 1f);
        public static readonly NumVector4 Yellow = new NumVector4(1f, 1f, 0f, 1f);
        public static readonly NumVector4 Green = new NumVector4(0f, 1f, 0f, 1f);
        public static readonly NumVector4 Orange = new NumVector4(1f, 0.5f, 0f, 1f);

        private float timer = 0f;
        private long cachedQueued = 0;
        private long cachedCompleted = 0;
        private long cachedFailed = 0;
        private int cachedPending = 0;
        private int cachedActive = 0;

        public override void OnGUIText()
        {
            timer += Time.Delta;
            if (timer >= 1f)
            {
                cachedActive = WorkerPoolManager.ActiveThreads;
                cachedQueued = WorkerPoolManager.TasksQueued;
                cachedCompleted = WorkerPoolManager.TasksCompleted;
                cachedFailed = WorkerPoolManager.TasksFailed;
                cachedPending = WorkerPoolManager.PendingTasks;
                timer = 0f;
            }

            ImGui.SeparatorText("[THREADS]");
            var colActive = cachedActive == 0 ? White : Green;
            var colQueued = cachedQueued == 0 ? White : Yellow;
            var colCompleted = cachedCompleted == 0 ? White : Green;
            var colFailed = cachedFailed == 0 ? White : Red;
            var colPending = cachedPending == 0 ? White : Orange;

            ImGui.TextColored(colActive, $"Active workers: {cachedActive}");
            ImGui.TextColored(colQueued, $"Queued: {cachedQueued}"); ImGui.SameLine();
            ImGui.TextColored(colCompleted, $" | Completed: {cachedCompleted}");
           
            ImGui.TextColored(colPending, $"Pending: {cachedPending}");  ImGui.SameLine();
            ImGui.TextColored(colFailed, $" | Failed: {cachedFailed}");
           
        }
    }
}
