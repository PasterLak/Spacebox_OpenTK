namespace Engine.Tests.Core;

public class TickTaskManagerTests : IDisposable
{
    public void Dispose()
    {
        TickTaskManager.Dispose();
    }

    [Fact]
    public void StoppedTask_IsRemovedWithoutRemovingOthers()
    {
        int ticks = 0;
        var stopped = new TickTask(1, false);
        var running = new TickTask(100, false) { OnTick = () => ticks++ };
        TickTaskManager.AddTask(running);
        TickTaskManager.AddTask(stopped);
        stopped.Stop();

        TickTaskManager.UpdateTasks();
        TickTaskManager.UpdateTasks();

        Assert.Equal(2, ticks);
    }

    [Fact]
    public void TasksAddedFromWorkers_AreAllUpdated()
    {
        int completed = 0;
        Parallel.For(0, 200, _ => TickTaskManager.AddTask(new TickTask(1, () => Interlocked.Increment(ref completed), false)));

        TickTaskManager.UpdateTasks();

        Assert.Equal(200, completed);
    }
}
