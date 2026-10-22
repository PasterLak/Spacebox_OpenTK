using Spacebox.Network.Statistics;

namespace Spacebox.Tests.Network;

public sealed class ServerStatisticsTests
{
    private static readonly DateTime Day1 = new DateTime(2026, 10, 20, 18, 0, 0);

    [Fact]
    public void Peaks_TrackTodayAndAllTime()
    {
        var statistics = new ServerStatistics();
        statistics.PlayerJoined("Anna", 1, Day1);
        statistics.PlayerJoined("Bob", 2, Day1.AddMinutes(5));
        statistics.PlayerJoined("Cid", 1, Day1.AddDays(1));

        Assert.Equal(2, statistics.PeakOn(Day1));
        Assert.Equal(1, statistics.PeakOn(Day1.AddDays(1)));
        Assert.Equal(2, statistics.Data.AllTimePeak);
        Assert.StartsWith("2026-10-20T18:05", statistics.Data.AllTimePeakAt);
    }

    [Fact]
    public void Joins_CountEveryJoinButPlayersOnce()
    {
        var statistics = new ServerStatistics();
        statistics.PlayerJoined("Anna", 1, Day1);
        statistics.PlayerJoined("anna", 1, Day1.AddHours(1));
        statistics.PlayerJoined("Bob", 2, Day1.AddHours(1));

        Assert.Equal(3, statistics.Data.TotalJoins);
        Assert.Equal(2, statistics.UniquePlayers);
    }

    [Fact]
    public void Times_AddUpAndIgnoreInvalidValues()
    {
        var statistics = new ServerStatistics();
        statistics.PlayerLeft(3600);
        statistics.PlayerLeft(-5);
        statistics.PlayerLeft(double.NaN);
        statistics.AddUptime(7200);

        Assert.Equal(3600, statistics.Data.PlayerSeconds);
        Assert.Equal(7200, statistics.Data.UptimeSeconds);
        Assert.Equal("1 h 0 min", ServerStatisticsReport.Hours(3600));
        Assert.Equal("5 min", ServerStatisticsReport.Hours(300));
    }

    [Fact]
    public void OldDays_AreDroppedAndRecentDaysAreFilled()
    {
        var statistics = new ServerStatistics();
        for (int i = 0; i < 80; i++) statistics.RecordOnline(1 + i % 3, Day1.AddDays(i));

        Assert.True(statistics.Data.DailyPeaks.Count <= ServerStatistics.KeptDays);
        var recent = statistics.RecentDays(Day1.AddDays(81), 3);
        Assert.Equal(3, recent.Count);
        Assert.Equal(0, recent[^1].Value);
        Assert.Equal(1 + 79 % 3, recent[0].Value);
    }

    [Fact]
    public void SavedData_IsRestored()
    {
        var first = new ServerStatistics();
        first.PlayerJoined("Anna", 3, Day1);
        var restored = new ServerStatistics(first.Data);

        restored.PlayerJoined("Anna", 1, Day1);
        Assert.Equal(1, restored.UniquePlayers);
        Assert.Equal(3, restored.Data.AllTimePeak);
    }
}
