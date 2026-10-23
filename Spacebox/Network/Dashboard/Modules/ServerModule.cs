using System.Runtime.InteropServices;
using Engine;
using Engine.Networking;
using Engine.Web;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Network.Dedicated;
using Spacebox.Network.Discovery;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class ServerModule : IDashboardModule
{
    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 600;
    public const int MaxPlayersLimit = 128;

    public sealed class SettingsRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxPlayers { get; set; }
        public bool LanDiscovery { get; set; }
        public float AutoSaveInterval { get; set; }
        public int UpdateRate { get; set; }
    }

    private sealed class IconRequest
    {
        public string Data { get; set; }
    }

    public void Map(HttpRouter router, DashboardContext context)
    {
        var guard = context.Guard;

        router.Map("GET", DashboardContext.Api("/server"), guard.Admin(async (_, _) => HttpResponse.Json(await MainThread.Run(() => Info(context)))));

        router.Map("GET", DashboardContext.Api("/metrics"), guard.Admin((_, _) => HttpResponse.Json(new
        {
            latest = context.Metrics.Latest,
            history = context.Metrics.History(),
            processors = Environment.ProcessorCount,
            uptimeSeconds = (long)(DateTime.Now - DashboardMetrics.StartTime).TotalSeconds
        })));

        router.Map("POST", DashboardContext.Api("/server/settings"), guard.Admin(async (request, user) =>
        {
            var body = DashboardGuard.Read<SettingsRequest>(request);
            if (!Validate(body, out var error)) return HttpResponse.Error(error, 400);

            await MainThread.Run(() => Apply(context.Config, body));
            DashboardContext.Audit(user, "changed the server settings");
            return HttpResponse.Json(new { ok = true });
        }));

        router.Map("POST", DashboardContext.Api("/server/save"), guard.Admin(async (_, user) =>
        {
            DashboardContext.Audit(user, "saved the world");
            await MainThread.Run(() => ServerRestart.Save("Dashboard"));
            return HttpResponse.Json(new { ok = true });
        }));

        router.Map("POST", DashboardContext.Api("/server/stop"), guard.Admin((request, user) =>
        {
            DashboardContext.Audit(user, "stopped the server");
            _ = MainThread.Run(ServerRestart.Stop);
            return HttpResponse.Json(new { ok = true });
        }));

        router.Map("POST", DashboardContext.Api("/server/restart"), guard.Admin((request, user) =>
        {
            DashboardContext.Audit(user, "restarted the server");
            _ = MainThread.Run(() => ServerRestart.Restart());
            return HttpResponse.Json(new { ok = true });
        }));

        router.Map("POST", DashboardContext.Api("/server/say"), guard.Admin(async (request, user) =>
        {
            string text = DashboardGuard.Read<TextRequest>(request)?.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return HttpResponse.Error("Enter a message", 400);
            if (text.Length > 256) text = text.Substring(0, 256);

            await MainThread.Run(() => ChatService.Broadcast("[Server]: " + text, Color4.Orange));
            return HttpResponse.Json(new { ok = true });
        }));

        router.Map("GET", DashboardContext.Api("/server/icon"), guard.Admin((_, _) =>
        {
            var icon = ServerIconFile.TryRead(context.Config.IconFile);
            return icon == null ? HttpResponse.NotFound() : HttpResponse.Bytes(icon, ServerIconFile.IsPng(icon) ? "image/png" : "image/jpeg").WithHeader("Cache-Control", "no-store");
        }));

        router.Map("POST", DashboardContext.Api("/server/icon"), guard.Admin(async (request, user) =>
        {
            byte[] data;
            try
            {
                data = Convert.FromBase64String(DashboardGuard.Read<IconRequest>(request)?.Data ?? "");
            }
            catch (FormatException)
            {
                return HttpResponse.Error("Bad image data", 400);
            }

            if (!ServerIconFile.IsValid(data)) return HttpResponse.Error($"The icon must be a PNG or JPEG up to {ServerIconFile.MaxBytes / 1024} KB", 400);

            await MainThread.Run(() => SaveIcon(context.Config, data));
            DashboardContext.Audit(user, "changed the server icon");
            return HttpResponse.Json(new { ok = true });
        }));
    }

    public static bool Validate(SettingsRequest body, out string error)
    {
        error = null;
        if (body == null) error = "Bad request";
        else if (string.IsNullOrWhiteSpace(body.Name) || body.Name.Length > MaxNameLength) error = $"The name needs 1 to {MaxNameLength} characters";
        else if ((body.Description?.Length ?? 0) > MaxDescriptionLength) error = $"The description can have at most {MaxDescriptionLength} characters";
        else if (body.MaxPlayers < 1 || body.MaxPlayers > MaxPlayersLimit) error = $"Max players must be 1 to {MaxPlayersLimit}";
        else if (body.AutoSaveInterval != 0 && body.AutoSaveInterval < AutoSaveTimer.MinInterval) error = $"Autosave must be 0 (off) or at least {AutoSaveTimer.MinInterval} seconds";
        else if (body.UpdateRate < ServerConfig.MinUpdateRate || body.UpdateRate > ServerConfig.MaxUpdateRate) error = $"The update rate must be {ServerConfig.MinUpdateRate} to {ServerConfig.MaxUpdateRate}";
        return error == null;
    }

    public const int TopPlayers = 10;

    private static object Statistics(PlayerPlayTimes playTimes)
    {
        var statistics = Spacebox.Network.Statistics.ServerStatisticsService.Current;
        if (statistics == null) return null;

        var data = statistics.Data;
        var now = DateTime.Now;
        return new
        {
            today = statistics.PeakOn(now),
            allTime = data.AllTimePeak,
            allTimeAt = DateTime.TryParse(data.AllTimePeakAt, out var at) ? at.ToString("yyyy-MM-dd HH:mm") : "",
            joins = data.TotalJoins,
            uniquePlayers = statistics.UniquePlayers,
            playTime = Spacebox.Network.Statistics.ServerStatisticsReport.Hours(data.PlayerSeconds),
            averageSession = Spacebox.Network.Statistics.ServerStatisticsReport.Hours(PlayTimeSummary.AverageSession(data.PlayerSeconds, data.TotalJoins)),
            averagePerPlayer = Spacebox.Network.Statistics.ServerStatisticsReport.Hours(PlayTimeSummary.AveragePerPlayer(data.PlayerSeconds, statistics.UniquePlayers)),
            topPlayers = PlayTimeSummary.Top(playTimes.All(World.WorldData?.WorldFolderPath), TopPlayers)
                .Select(player => new { name = player.Name, playSeconds = Math.Round(player.Seconds), sessions = player.Sessions }),
            runningTime = Spacebox.Network.Statistics.ServerStatisticsReport.Hours(data.UptimeSeconds),
            days = statistics.RecentDays(now, 14).Select(day => new { day = day.Key, peak = day.Value })
        };
    }

    private static object Info(DashboardContext context)
    {
        var config = context.Config;
        var info = World.WorldData?.Info;
        return new
        {
            name = config.ServerName,
            description = config.Description,
            version = Application.Version,
            world = info?.Name ?? config.WorldName,
            seed = info?.Seed ?? config.Seed,
            modId = info?.ModId ?? config.ModId,
            gameMode = (info?.GameMode ?? 0).ToString(),
            port = config.Port,
            dashboardPort = config.GetDashboardPort(),
            players = PlayerRegistry.All.Count,
            maxPlayers = config.MaxPlayers,
            statistics = Statistics(context.PlayTimes),
            lanDiscovery = config.LanDiscovery,
            autoSaveInterval = config.AutoSaveInterval,
            updateRate = config.UpdateRate,
            whitelistEnabled = config.WhitelistEnabled,
            registrationEnabled = config.RegistrationEnabled,
            https = DashboardService.HttpsStatus,
            os = RuntimeInformation.OSDescription,
            runtime = RuntimeInformation.FrameworkDescription,
            processors = Environment.ProcessorCount,
            worldSizeBytes = FolderSize(World.WorldData?.WorldFolderPath),
            targetTps = (int)Math.Round(Time.TargetTPS),
            startedAt = DashboardMetrics.StartTime.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private static void Apply(ServerConfig config, SettingsRequest body)
    {
        config.ServerName = body.Name.Trim();
        config.Description = body.Description?.Trim() ?? "";
        config.MaxPlayers = body.MaxPlayers;
        config.LanDiscovery = body.LanDiscovery;
        config.AutoSaveInterval = body.AutoSaveInterval;
        config.UpdateRate = body.UpdateRate;
        config.Save();

        NetworkManager.Instance.MaxPeers = Math.Max(1, config.MaxPlayers);
        FrameLimiter.TargetFPS = config.GetClampedUpdateRate();
        NetworkSession.Current?.RefreshAdvertisement();
    }

    private static void SaveIcon(ServerConfig config, byte[] data)
    {
        string extension = ServerIconFile.IsPng(data) ? ".png" : ".jpg";
        string current = ServerIconFile.ResolvePath(config.IconFile);
        if (current != null) File.Delete(current);

        string fileName = Path.ChangeExtension(string.IsNullOrWhiteSpace(config.IconFile) ? "server-icon" : config.IconFile, extension);
        string path = Path.IsPathRooted(fileName) ? fileName : MultiplayerFolders.File(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, data);

        config.IconFile = fileName;
        config.Save();
        NetworkSession.Current?.RefreshAdvertisement();
    }

    private static long FolderSize(string folder)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return 0;

        long total = 0;
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            try
            {
                total += new FileInfo(file).Length;
            }
            catch (IOException)
            {
            }
        }
        return total;
    }
}
