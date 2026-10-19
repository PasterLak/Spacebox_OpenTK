using Spacebox.Game.Player;
using Spacebox.Game.Tutorial;
using Xunit;

namespace Spacebox.Tests.Tutorial;

public class TutorialGameHelpersTests
{
    [Fact]
    public void StatReader_ReadsNumbersAndCollections()
    {
        var stats = new PlayerStatistics { BlocksPlaced = 7, DistanceTraveled = 12.5 };
        stats.VisitedAsteroids.Add(3);
        stats.VisitedAsteroids.Add(4);

        Assert.Equal(7, PlayerStatReader.Read(stats, "BlocksPlaced"));
        Assert.Equal(12.5, PlayerStatReader.Read(stats, "distancetraveled"));
        Assert.Equal(2, PlayerStatReader.Read(stats, nameof(PlayerStatistics.VisitedAsteroids)));
        Assert.Equal(0, PlayerStatReader.Read(stats, "NoSuchStat"));
        Assert.False(PlayerStatReader.IsKnown("FirstPlayedUtc"));
        Assert.True(PlayerStatReader.IsKnown("ShotsFired"));
    }

    [Theory]
    [InlineData("Mouse Button1", "LMB")]
    [InlineData("Mouse Button2", "RMB")]
    [InlineData("LeftControl", "Ctrl")]
    [InlineData("LeftShift", "Shift")]
    [InlineData("D3", "3")]
    [InlineData("W", "W")]
    [InlineData("", "?")]
    public void KeyNames_ArePrettified(string raw, string expected)
    {
        Assert.Equal(expected, Spacebox.Game.GUI.KeyNames.Pretty(raw));
    }
}
