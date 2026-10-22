namespace Spacebox.Network.Statistics;

public sealed class ServerStatisticsData
{
    public int AllTimePeak { get; set; }
    public string AllTimePeakAt { get; set; } = "";
    public Dictionary<string, int> DailyPeaks { get; set; } = new Dictionary<string, int>();
    public long TotalJoins { get; set; }
    public List<string> KnownPlayers { get; set; } = new List<string>();
    public double PlayerSeconds { get; set; }
    public double UptimeSeconds { get; set; }
    public string FirstStarted { get; set; } = "";
}

public sealed class ServerStatistics
{
    public const int KeptDays = 60;
    private const string DayFormat = "yyyy-MM-dd";

    private readonly HashSet<string> _known;

    public ServerStatistics(ServerStatisticsData data = null)
    {
        Data = data ?? new ServerStatisticsData();
        Data.DailyPeaks ??= new Dictionary<string, int>();
        Data.KnownPlayers ??= new List<string>();
        _known = new HashSet<string>(Data.KnownPlayers, StringComparer.OrdinalIgnoreCase);
    }

    public ServerStatisticsData Data { get; }
    public int UniquePlayers => _known.Count;

    public void Started(DateTime now)
    {
        if (string.IsNullOrEmpty(Data.FirstStarted)) Data.FirstStarted = now.ToString("O");
    }

    public void PlayerJoined(string name, int onlineCount, DateTime now)
    {
        Data.TotalJoins++;
        if (!string.IsNullOrWhiteSpace(name) && _known.Add(name.Trim())) Data.KnownPlayers.Add(name.Trim());
        RecordOnline(onlineCount, now);
    }

    public void PlayerLeft(double sessionSeconds)
    {
        if (double.IsFinite(sessionSeconds) && sessionSeconds > 0) Data.PlayerSeconds += sessionSeconds;
    }

    public void AddUptime(double seconds)
    {
        if (double.IsFinite(seconds) && seconds > 0) Data.UptimeSeconds += seconds;
    }

    public void RecordOnline(int onlineCount, DateTime now)
    {
        if (onlineCount <= 0) return;

        string day = now.ToString(DayFormat);
        if (!Data.DailyPeaks.TryGetValue(day, out int peak) || onlineCount > peak) Data.DailyPeaks[day] = onlineCount;
        if (onlineCount > Data.AllTimePeak)
        {
            Data.AllTimePeak = onlineCount;
            Data.AllTimePeakAt = now.ToString("O");
        }
        TrimDays(now);
    }

    public int PeakOn(DateTime day) => Data.DailyPeaks.TryGetValue(day.ToString(DayFormat), out int peak) ? peak : 0;

    public IReadOnlyList<KeyValuePair<string, int>> RecentDays(DateTime now, int days)
    {
        var result = new List<KeyValuePair<string, int>>(days);
        for (int i = days - 1; i >= 0; i--)
        {
            string day = now.AddDays(-i).ToString(DayFormat);
            result.Add(new KeyValuePair<string, int>(day, Data.DailyPeaks.TryGetValue(day, out int peak) ? peak : 0));
        }
        return result;
    }

    private void TrimDays(DateTime now)
    {
        if (Data.DailyPeaks.Count <= KeptDays) return;

        string oldest = now.AddDays(-KeptDays + 1).ToString(DayFormat);
        var old = new List<string>();
        foreach (var day in Data.DailyPeaks.Keys)
            if (string.CompareOrdinal(day, oldest) < 0) old.Add(day);
        foreach (var day in old) Data.DailyPeaks.Remove(day);
    }
}
