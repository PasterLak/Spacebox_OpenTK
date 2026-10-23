using System.Text.Json;
using System.Text.Json.Nodes;
using Spacebox.Game.Player;

namespace Spacebox.Network.Dashboard;

public readonly record struct PlayerPlayTime(string Name, double Seconds, int Sessions)
{
    public double AverageSessionSeconds => Sessions > 0 ? Seconds / Sessions : 0;
}

public sealed class PlayerPlayTimes
{
    private readonly Dictionary<string, (DateTime Saved, PlayerPlayTime Time)> _cache = new Dictionary<string, (DateTime, PlayerPlayTime)>(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new object();

    public PlayerPlayTime Get(string worldFolder, string name)
    {
        var saved = PlayerFiles.LastSaved(worldFolder, name);
        lock (_lock)
        {
            if (_cache.TryGetValue(name, out var cached) && cached.Saved == saved) return cached.Time;
        }

        var time = Parse(name, PlayerSaveLoadManager.ReadPlayerJson(worldFolder, name, false));
        lock (_lock) _cache[name] = (saved, time);
        return time;
    }

    public List<PlayerPlayTime> All(string worldFolder)
    {
        var result = new List<PlayerPlayTime>();
        if (string.IsNullOrEmpty(worldFolder)) return result;
        foreach (var name in PlayerFiles.SavedPlayerNames(worldFolder)) result.Add(Get(worldFolder, name));
        return result;
    }

    public static PlayerPlayTime Parse(string name, string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new PlayerPlayTime(name, 0, 0);

        try
        {
            if (JsonNode.Parse(json) is not JsonObject root || root["Statistics"] is not JsonObject statistics) return new PlayerPlayTime(name, 0, 0);
            double seconds = Number(statistics, "PlayTimeSeconds");
            if (seconds <= 0) seconds = Number(statistics, "TotalPlayTimeMinutes") * 60;
            return new PlayerPlayTime(name, Math.Max(0, seconds), (int)Math.Max(0, Number(statistics, "SessionsPlayed")));
        }
        catch (JsonException)
        {
            return new PlayerPlayTime(name, 0, 0);
        }
    }

    private static double Number(JsonObject node, string key)
    {
        try
        {
            double value = node[key]?.GetValue<double>() ?? 0;
            return double.IsFinite(value) ? value : 0;
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            return 0;
        }
    }
}

public static class PlayTimeSummary
{
    public static List<PlayerPlayTime> Top(IEnumerable<PlayerPlayTime> players, int count)
    {
        var sorted = new List<PlayerPlayTime>();
        foreach (var player in players)
        {
            if (player.Seconds > 0) sorted.Add(player);
        }
        sorted.Sort((a, b) => b.Seconds != a.Seconds ? b.Seconds.CompareTo(a.Seconds) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        if (sorted.Count > count) sorted.RemoveRange(count, sorted.Count - count);
        return sorted;
    }

    public static double AverageSession(double totalSeconds, long sessions) => sessions > 0 ? totalSeconds / sessions : 0;

    public static double AveragePerPlayer(double totalSeconds, int players) => players > 0 ? totalSeconds / players : 0;
}
