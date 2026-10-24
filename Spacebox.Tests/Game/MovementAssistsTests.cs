using System.Text.Json;
using Spacebox.Game;
using Spacebox.Game.Player;

namespace Spacebox.Tests.Game;

public class MovementAssistsTests
{
    [Fact]
    public void WithoutToggle_SprintFollowsTheHeldKey()
    {
        var toggle = new SprintToggle();
        Assert.True(toggle.Update(false, true, true, true));
        Assert.False(toggle.Update(false, false, false, true));
    }

    [Fact]
    public void Toggle_PressStartsAndPressAgainStops()
    {
        var toggle = new SprintToggle();
        Assert.True(toggle.Update(true, true, true, true));
        Assert.True(toggle.Update(true, false, false, true));
        Assert.False(toggle.Update(true, true, true, true));
        Assert.False(toggle.Update(true, false, false, true));
    }

    [Fact]
    public void Toggle_StopsWhenMovementStops()
    {
        var toggle = new SprintToggle();
        toggle.Update(true, true, true, true);
        Assert.False(toggle.Update(true, false, false, false));
        Assert.False(toggle.Update(true, false, false, true));
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
