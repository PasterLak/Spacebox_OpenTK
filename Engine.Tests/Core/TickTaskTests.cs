namespace Engine.Tests.Core;

public class TickTaskTests
{
    [Fact]
    public void Completes_AfterRequiredTicks()
    {
        int completed = 0;
        var task = new TickTask(3, () => completed++, false);

        task.Update();
        task.Update();
        Assert.Equal(0, completed);
        Assert.True(task.IsRunning);

        task.Update();
        Assert.Equal(1, completed);
        Assert.True(task.IsComplete);
    }

    [Fact]
    public void Loop_RestartsAfterCompletion()
    {
        int completed = 0;
        var task = new TickTask(2, () => completed++, true);

        for (int i = 0; i < 6; i++) task.Update();

        Assert.Equal(3, completed);
        Assert.False(task.IsComplete);
    }

    [Fact]
    public void Pause_StopsProgressUntilContinue()
    {
        int ticks = 0;
        var task = new TickTask(5, false) { OnTick = () => ticks++ };

        task.Update();
        task.Pause();
        task.Update();
        task.Update();
        Assert.Equal(1, ticks);
        Assert.True(task.IsPaused);

        task.Continue();
        task.Update();
        Assert.Equal(2, ticks);
    }

    [Fact]
    public void Stop_PreventsFurtherUpdates()
    {
        int completed = 0;
        var task = new TickTask(1, () => completed++, true);

        task.Stop();
        task.Update();

        Assert.True(task.IsStopped);
        Assert.Equal(0, completed);
    }

    [Fact]
    public void SetCurrentTick_CompletesWhenReachingRequired()
    {
        int completed = 0;
        var task = new TickTask(10, () => completed++, false);

        task.SetCurrentTick(9);
        Assert.Equal(0, completed);

        task.SetCurrentTick(10);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void SetRequiredTicks_BelowCurrentCompletes()
    {
        int completed = 0;
        var task = new TickTask(10, () => completed++, false);
        for (int i = 0; i < 4; i++) task.Update();

        task.SetRequiredTicks(3);

        Assert.Equal(1, completed);
    }
}
