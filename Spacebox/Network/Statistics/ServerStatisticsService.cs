using Stopwatch = System.Diagnostics.Stopwatch;
using Engine;
using Spacebox.Game.Generation.Tools.SaveData;

namespace Spacebox.Network.Statistics;

public static class ServerStatisticsService
{
    public const string FileName = "statistics.nbt";

    private static readonly Stopwatch Uptime = new Stopwatch();
    private static ServerStatistics _current;

    public static ServerStatistics Current => _current;
    public static bool IsActive => _current != null;
    private static string FilePath => MultiplayerFolders.File(FileName);

    public static void Start()
    {
        if (_current != null) return;

        ServerStatisticsData data = null;
        try
        {
            data = SaveDataFile.Read<ServerStatisticsData>(FilePath);
        }
        catch (Exception ex)
        {
            Debug.Warning($"[Statistics] {FileName} could not be read, starting new statistics: {ex.Message}");
        }

        _current = new ServerStatistics(data);
        _current.Started(DateTime.Now);
        Uptime.Restart();
    }

    public static void PlayerJoined(string name, int onlineCount)
    {
        if (_current == null) return;
        _current.PlayerJoined(name, onlineCount, DateTime.Now);
        Save();
    }

    public static void PlayerLeft(DateTime joinedAtUtc)
    {
        if (_current == null) return;
        _current.PlayerLeft((DateTime.UtcNow - joinedAtUtc).TotalSeconds);
        Save();
    }

    public static void Save()
    {
        if (_current == null) return;

        _current.AddUptime(Uptime.Elapsed.TotalSeconds);
        Uptime.Restart();
        try
        {
            SaveDataFile.Write(FilePath, _current.Data);
        }
        catch (Exception ex)
        {
            Debug.Warning($"[Statistics] Could not save {FileName}: {ex.Message}");
        }
    }
}
