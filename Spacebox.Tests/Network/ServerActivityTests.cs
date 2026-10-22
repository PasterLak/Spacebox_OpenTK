using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Network;

public sealed class ServerActivityTests
{
    [Fact]
    public void EmptyServer_GoesIdleThenSleeps()
    {
        var activity = new ServerActivity(60, 10, 5f);

        Assert.Equal(ServerActivityState.Active, activity.Update(0, 10f));
        Assert.Equal(60, activity.TargetRate);
        Assert.Equal(ServerActivityState.Idle, activity.Update(0, 25f));
        Assert.Equal(10, activity.TargetRate);
        Assert.Equal(ServerActivityState.Sleep, activity.Update(0, 300f));
    }

    [Fact]
    public void AnyConnection_WakesTheRateAndResetsTheTimer()
    {
        var activity = new ServerActivity(60, 10, 5f);
        activity.Update(0, 200f);

        Assert.Equal(ServerActivityState.Active, activity.Update(1, 0.1f));
        Assert.Equal(60, activity.TargetRate);
        Assert.Equal(ServerActivityState.Active, activity.Update(0, 10f));
    }

    [Fact]
    public void ZeroSettings_TurnIdleRateAndSleepOff()
    {
        var activity = new ServerActivity(60, 0, 0f);

        Assert.Equal(ServerActivityState.Idle, activity.Update(0, 100000f));
        Assert.Equal(60, activity.TargetRate);
    }
}
