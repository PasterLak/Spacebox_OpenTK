using Engine;
using Engine.Multithreading;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.Backups;

public static class WorldBackupService
{
    public const string AutoLabel = "auto";
    public const string ManualLabel = "manual";
    public const string StopLabel = "stop";

    private static readonly string[] RotatedLabels = { AutoLabel, StopLabel };
    private static int _running;

    public static bool IsRunning => Volatile.Read(ref _running) > 0;

    public static bool TryStart(string worldFolder, string label, int keep, Action<string, string> done)
    {
        if (string.IsNullOrEmpty(worldFolder) || !Directory.Exists(worldFolder)) return false;
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return false;

        WorkerPoolManager.Enqueue(_ =>
        {
            string path = null;
            string error = null;
            try
            {
                path = CreateNow(worldFolder, label, keep);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
            EngineWindow._mainThreadActions.Enqueue(() => done?.Invoke(path, error));
        }, WorkerPoolManager.Priority.Low);
        return true;
    }

    public static string CreateNow(string worldFolder, string label, int keep)
    {
        string path = WorldBackup.Create(worldFolder, label, null, DateTime.Now);
        Prune(worldFolder, keep);
        return path;
    }

    public static void Prune(string worldFolder, int keep)
    {
        if (keep <= 0) return;

        string directory = WorldBackup.DefaultDirectory(worldFolder);
        if (!Directory.Exists(directory)) return;

        string worldName = Path.GetFileName(Path.GetFullPath(worldFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var names = new List<string>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.zip")) names.Add(Path.GetFileName(file));

        foreach (string name in BackupRetention.SelectToDelete(names, worldName, RotatedLabels, keep))
        {
            try
            {
                File.Delete(Path.Combine(directory, name));
            }
            catch (IOException ex)
            {
                Debug.Warning($"[Backup] Could not delete the old backup {name}: {ex.Message}");
            }
        }
    }

    public static string Describe(string path)
    {
        long size = File.Exists(path) ? new FileInfo(path).Length : 0;
        return $"{Path.GetFileName(path)} ({size / 1024 / 1024.0:0.0} MB)";
    }
}
