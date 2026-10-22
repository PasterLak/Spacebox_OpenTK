using Engine;
using Engine.Commands;
using OpenTK.Mathematics;
using Spacebox.Game.Backups;
using Spacebox.Game.Generation;
using Spacebox.Network;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands;

public sealed class BackupCommand : CommandBase
{
    public override string Name => "backup";
    public override string Description => "Save the world and write a zip backup to Worlds/_backups";

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        string folder = World.WorldData?.WorldFolderPath;
        if (string.IsNullOrEmpty(folder))
        {
            Debug.Error("No world is loaded");
            return;
        }

        if (NetworkSession.Current != null) NetworkSession.Current.SaveWorld("Backup");
        else World.Instance?.Save(false);

        string sender = ChatCommands.RemoteSender;
        int keep = Spacebox.Network.Dedicated.DedicatedServer.Config?.Backups?.KeepLast ?? 0;
        if (!WorldBackupService.TryStart(folder, WorldBackupService.ManualLabel, keep, (path, error) => Report(sender, path, error)))
        {
            Debug.Warning("A backup is already being written");
            return;
        }

        Debug.Log("Writing the backup...");
    }

    private static void Report(string sender, string path, string error)
    {
        string text = error == null ? $"Backup saved: {WorldBackupService.Describe(path)}" : $"Backup failed: {error}";
        if (error == null) Debug.Success("[Backup] " + text);
        else Debug.Error("[Backup] " + text);

        var color = error == null ? Color4.LightGreen : Color4.OrangeRed;
        if (sender != null) ChatService.SendToPlayer(sender, text, color);
        else if (GameSession.HasLocalPlayer) Spacebox.Game.GUI.Chat.Write(text, color);
    }
}
