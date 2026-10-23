using System.Text.Json;
using Spacebox.Game;
using Spacebox.Game.Player;

namespace Spacebox.Tests.Game;

public class MovementAssistsTests
{
    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, true, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    public void Sprint_HeldOrAutoWhileMovingForward(bool held, bool auto, bool forward, bool expected)
    {
        Assert.Equal(expected, MovementAssists.IsSprinting(held, auto, forward));
    }

    [Fact]
    public void AutoJump_NeedsEveryCondition()
    {
        Assert.True(MovementAssists.ShouldAutoJump(true, true, true, false, true, true));
        Assert.False(MovementAssists.ShouldAutoJump(false, true, true, false, true, true));
        Assert.False(MovementAssists.ShouldAutoJump(true, false, true, false, true, true));
        Assert.False(MovementAssists.ShouldAutoJump(true, true, true, true, true, true));
        Assert.False(MovementAssists.ShouldAutoJump(true, true, true, false, false, true));
        Assert.False(MovementAssists.ShouldAutoJump(true, true, true, false, true, false));
    }

    [Fact]
    public void Assists_AreOffByDefaultAndLoadFromOldSettings()
    {
        var defaults = new GameplaySettings();
        Assert.False(defaults.AutoSprint);
        Assert.False(defaults.AutoJump);

        var old = JsonSerializer.Deserialize<GameplaySettings>("{\"draw_distance\":500}");
        Assert.False(old.AutoSprint);
        Assert.False(old.AutoJump);

        var saved = JsonSerializer.Deserialize<GameplaySettings>("{\"auto_sprint\":true,\"auto_jump\":true}");
        Assert.True(saved.AutoSprint);
        Assert.True(saved.AutoJump);
    }
}
