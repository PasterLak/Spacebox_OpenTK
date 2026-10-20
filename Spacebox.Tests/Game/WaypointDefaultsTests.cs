using Spacebox.Game.Waypoints;
using Xunit;

namespace Spacebox.Tests.Game;

public class WaypointDefaultsTests
{
    [Fact]
    public void NextName_UsesTheFirstFreeNumber()
    {
        Assert.Equal("Waypoint 1", WaypointDefaults.NextName(new string[0]));
        Assert.Equal("Waypoint 2", WaypointDefaults.NextName(new[] { "waypoint 1", "Home" }));
        Assert.Equal("Waypoint 3", WaypointDefaults.NextName(new[] { "Waypoint 1", "Waypoint 2", "Waypoint 4" }));
        Assert.Equal("Waypoint 1", WaypointDefaults.NextName(null));
    }

    [Fact]
    public void CleanName_TrimsLimitsAndFallsBack()
    {
        Assert.Equal("Home", WaypointDefaults.CleanName("  Home \n", "x"));
        Assert.Equal("x", WaypointDefaults.CleanName("   ", "x"));
        Assert.Equal(WaypointDefaults.MaxNameLength, WaypointDefaults.CleanName(new string('a', 100), "x").Length);
    }

    [Fact]
    public void ColorFor_CyclesThroughThePalette()
    {
        int count = WaypointDefaults.Palette.Length;

        Assert.Equal(WaypointDefaults.Palette[0], WaypointDefaults.ColorFor(0));
        Assert.Equal(WaypointDefaults.Palette[1], WaypointDefaults.ColorFor(count + 1));
        Assert.Equal(WaypointDefaults.Palette[count - 1], WaypointDefaults.ColorFor(-1));
    }
}
