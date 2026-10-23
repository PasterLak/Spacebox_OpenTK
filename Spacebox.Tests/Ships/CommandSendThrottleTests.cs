using OpenTK.Mathematics;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public sealed class CommandSendThrottleTests
{
    [Fact]
    public void SameCommand_IsOnlyResentAsKeepAlive()
    {
        var throttle = new CommandSendThrottle();
        var command = new ConsoleCommand { Buttons = 1 };

        Assert.True(throttle.ShouldSend(command, 0f));
        Assert.False(throttle.ShouldSend(command, 0.016f));
        Assert.False(throttle.ShouldSend(command, 0.2f));
        Assert.True(throttle.ShouldSend(command, 0.26f));
    }

    [Fact]
    public void InputChanges_GoOutAtOnce_AimChangesAtMost20PerSecond()
    {
        var throttle = new CommandSendThrottle();
        throttle.ShouldSend(new ConsoleCommand(), 0f);

        Assert.True(throttle.ShouldSend(new ConsoleCommand { Buttons = 2 }, 0.01f));
        Assert.False(throttle.ShouldSend(new ConsoleCommand { Buttons = 2, AimLocal = Vector3.UnitX }, 0.02f));
        Assert.True(throttle.ShouldSend(new ConsoleCommand { Buttons = 2, AimLocal = Vector3.UnitX }, 0.07f));
    }
}
