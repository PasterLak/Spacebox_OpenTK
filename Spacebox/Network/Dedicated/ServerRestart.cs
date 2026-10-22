using Engine;
using Debug = Engine.Debug;
using Spacebox.Game.Generation;

namespace Spacebox.Network.Dedicated;

public static class ServerRestart
{
    private static bool _stopping;

    public static void Save(string reason)
    {
        if (NetworkSession.Current != null) NetworkSession.Current.SaveWorld(reason);
        else World.Instance?.Save(false);
    }

    public static void Stop() => Stop(null);

    public static void Stop(Action completed)
    {
        if (_stopping) return;
        _stopping = true;
        Debug.Warning("[Server] Stopping: saving the world");
        FlushPlayersThen(() =>
        {
            try
            {
                Save("Stop");
                BackupOnStop();
                NetworkSession.Current?.Dispose();
                Application.Quit();
            }
            finally
            {
                completed?.Invoke();
            }
        });
    }

    public static bool Restart()
    {
        if (_stopping) return false;
        _stopping = true;
        Debug.Warning("[Server] Restarting: saving the world");
        FlushPlayersThen(() =>
        {
            Save("Restart");
            NetworkSession.Current?.Dispose();
            _stopping = false;
            DedicatedServer.Relaunch();
        });
        return true;
    }

    private static void BackupOnStop()
    {
        var settings = DedicatedServer.Config?.Backups;
        string folder = World.WorldData?.WorldFolderPath;
        if (settings == null || !settings.Enabled || !settings.OnStop || string.IsNullOrEmpty(folder)) return;

        try
        {
            string path = Spacebox.Game.Backups.WorldBackupService.CreateNow(folder, Spacebox.Game.Backups.WorldBackupService.StopLabel, settings.KeepLast);
            Debug.Success($"[Backup] Backup on stop saved: {Spacebox.Game.Backups.WorldBackupService.Describe(path)}");
        }
        catch (Exception ex)
        {
            Debug.Error($"[Backup] Backup on stop failed: {ex.Message}");
        }
    }

    private static void FlushPlayersThen(Action action)
    {
        if (NetworkSession.Current == null) action();
        else NetworkSession.Current.FlushPlayerData(() => EngineWindow._mainThreadActions.Enqueue(action));
    }
}
