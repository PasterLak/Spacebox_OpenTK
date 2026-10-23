using Engine;
using Engine.Web;
using Spacebox.Game.Backups;
using Spacebox.Game.Generation;
using Spacebox.Game.Versioning;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class BackupsModule : IDashboardModule
{
    public const int MaxListed = 50;

    public void Map(HttpRouter router, DashboardContext context)
    {
        var guard = context.Guard;

        router.Map("GET", DashboardContext.Api("/backups"), guard.Admin(async (_, _) => HttpResponse.Json(await MainThread.Run(List))));

        router.Map("POST", DashboardContext.Api("/backups/settings"), guard.Admin(async (request, user) =>
        {
            var settings = DashboardGuard.Read<Dedicated.ServerBackupSettings>(request);
            if (settings == null) return HttpResponse.Error("Bad request", 400);

            bool corrected = settings.Validate();
            await MainThread.Run(() =>
            {
                context.Config.Backups = settings;
                context.Config.Save();
            });
            DashboardContext.Audit(user, "changed the backup settings");
            return HttpResponse.Json(new { ok = true, corrected });
        }));

        router.Map("POST", DashboardContext.Api("/backups"), guard.Admin(async (_, user) =>
        {
            string error = await MainThread.Run(() => Start(user));
            return error == null ? HttpResponse.Json(new { ok = true }) : HttpResponse.Error(error, 409);
        }));
    }

    private static object List()
    {
        string world = World.WorldData?.WorldFolderPath;
        var backups = new List<object>();
        if (world != null)
        {
            string directory = WorldBackup.DefaultDirectory(world);
            string prefix = Path.GetFileName(Path.GetFullPath(world).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + "_";
            if (Directory.Exists(directory))
            {
                var files = new DirectoryInfo(directory).GetFiles("*.zip")
                    .Where(file => file.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .Take(MaxListed);
                foreach (var file in files)
                    backups.Add(new { name = file.Name, sizeMb = Math.Round(file.Length / 1024d / 1024d, 1), created = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm") });
            }
        }

        var settings = Dedicated.DedicatedServer.Config?.Backups;
        return new
        {
            running = WorldBackupService.IsRunning,
            enabled = settings?.Enabled ?? false,
            intervalMinutes = settings?.IntervalMinutes ?? 0,
            keepLast = settings?.KeepLast ?? 0,
            onStop = settings?.OnStop ?? false,
            minIntervalMinutes = Dedicated.ServerBackupSettings.MinIntervalMinutes,
            backups
        };
    }

    private static string Start(string user)
    {
        string folder = World.WorldData?.WorldFolderPath;
        if (folder == null) return "No world is loaded";
        if (WorldBackupService.IsRunning) return "A backup is already being written";

        NetworkSession.Current?.SaveWorld("Backup");
        int keep = Dedicated.DedicatedServer.Config?.Backups?.KeepLast ?? 0;
        bool started = WorldBackupService.TryStart(folder, WorldBackupService.ManualLabel, keep, (path, error) =>
        {
            if (error == null) Debug.Success($"[Backup] Backup saved: {WorldBackupService.Describe(path)}");
            else Debug.Error($"[Backup] Backup failed: {error}");
        });
        if (!started) return "The backup could not be started";

        DashboardContext.Audit(user, "started a backup");
        return null;
    }
}
