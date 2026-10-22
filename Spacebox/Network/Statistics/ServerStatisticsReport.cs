namespace Spacebox.Network.Statistics;

public static class ServerStatisticsReport
{
    public static IReadOnlyList<string> Lines(ServerStatistics statistics, int online, DateTime now)
    {
        var data = statistics.Data;
        return new[]
        {
            $"Players online: {online}",
            $"Most players today: {statistics.PeakOn(now)}",
            $"Most players ever: {data.AllTimePeak}{When(data.AllTimePeakAt)}",
            $"Joins: {data.TotalJoins}, different players: {statistics.UniquePlayers}",
            $"Time played by all players: {Hours(data.PlayerSeconds)}",
            $"Server running time: {Hours(data.UptimeSeconds)}{Since(data.FirstStarted)}"
        };
    }

    public static string Hours(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1 ? $"{(int)time.TotalHours} h {time.Minutes} min" : $"{time.Minutes} min";
    }

    private static string When(string iso) => DateTime.TryParse(iso, out var at) ? $" ({at:yyyy-MM-dd HH:mm})" : "";

    private static string Since(string iso) => DateTime.TryParse(iso, out var at) ? $" since {at:yyyy-MM-dd}" : "";
}
