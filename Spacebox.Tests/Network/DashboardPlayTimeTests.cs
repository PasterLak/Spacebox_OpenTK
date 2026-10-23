using Spacebox.Network.Dashboard;
using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Network;

public sealed class DashboardPlayTimeTests
{
    [Fact]
    public void PlayerSaves_GiveTimeAndSessions()
    {
        var time = PlayerPlayTimes.Parse("Ann", "{\"Health\":100,\"Statistics\":{\"PlayTimeSeconds\":7200.5,\"SessionsPlayed\":4}}");

        Assert.Equal(7200.5, time.Seconds);
        Assert.Equal(4, time.Sessions);
        Assert.Equal(1800.125, time.AverageSessionSeconds);
    }

    [Fact]
    public void OldSaves_UseMinutes_AndBrokenSavesCountAsZero()
    {
        Assert.Equal(600, PlayerPlayTimes.Parse("Old", "{\"Statistics\":{\"TotalPlayTimeMinutes\":10}}").Seconds);
        Assert.Equal(0, PlayerPlayTimes.Parse("Bad", "{not json").Seconds);
        Assert.Equal(0, PlayerPlayTimes.Parse("None", null).Seconds);
    }

    [Fact]
    public void Top_ListsTheLongestPlayersFirst()
    {
        var players = new[] { new PlayerPlayTime("a", 10, 1), new PlayerPlayTime("b", 300, 2), new PlayerPlayTime("c", 0, 0), new PlayerPlayTime("d", 50, 1) };

        var top = PlayTimeSummary.Top(players, 2);

        Assert.Equal(new[] { "b", "d" }, top.Select(p => p.Name));
        Assert.Equal(25, PlayTimeSummary.AverageSession(100, 4));
        Assert.Equal(0, PlayTimeSummary.AveragePerPlayer(100, 0));
    }

    [Fact]
    public void BackupSettings_AreKeptInRange()
    {
        var settings = new ServerBackupSettings { IntervalMinutes = 1, KeepLast = -3 };

        Assert.True(settings.Validate());
        Assert.Equal(ServerBackupSettings.MinIntervalMinutes, settings.IntervalMinutes);
        Assert.Equal(0, settings.KeepLast);
        Assert.False(new ServerBackupSettings().Validate());
    }
}
