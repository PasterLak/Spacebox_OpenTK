using Spacebox.Network;
using Xunit;

namespace Spacebox.Tests.Network;

public class AutoSaveTimerTests
{
    [Fact]
    public void Tick_FiresOncePerInterval()
    {
        var timer = new AutoSaveTimer();

        Assert.False(timer.Tick(59f, 60f));
        Assert.True(timer.Tick(1f, 60f));
        Assert.False(timer.Tick(30f, 60f));
        Assert.True(timer.Tick(30f, 60f));
    }

    [Fact]
    public void Tick_DisabledByZeroInterval()
    {
        var timer = new AutoSaveTimer();

        Assert.False(timer.Tick(10000f, 0f));
        Assert.False(timer.Tick(10000f, -5f));
    }

    [Fact]
    public void Tick_ClampsTinyIntervalsAndResets()
    {
        var timer = new AutoSaveTimer();

        Assert.False(timer.Tick(1f, 0.5f));
        Assert.True(timer.Tick(AutoSaveTimer.MinInterval, 0.5f));

        timer.Tick(5f, 60f);
        timer.Reset();
        Assert.Equal(0f, timer.Elapsed);
    }
}
