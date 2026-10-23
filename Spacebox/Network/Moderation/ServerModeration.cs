using System.Net;
using System.Text.Json;
using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Network.Moderation;

public static class ServerModeration
{
    public const string KickedReason = "You were kicked from the server";
    public const string BannedReason = "You are banned from this server";

    private const string FileName = "moderation.json";
    private static readonly Color4 NoticeColor = new Color4(1f, 0.6f, 0.2f, 1f);

    private static ModerationData _data = new ModerationData();
    private static string? _loadedFolder;

    public static bool IsBanned(string name, string address)
    {
        return Load().IsBanned(name, address);
    }

    public static bool IsMuted(string name)
    {
        return Load().IsMuted(name);
    }

    public static void Kick(int peer)
    {
        if (!CanModerate(peer, out var player)) return;

        NetworkManager.Instance.Kick(peer, KickedReason);
        ChatService.Broadcast($"{player.Name} was kicked", NoticeColor);
    }

    public static bool Ban(int peer)
    {
        if (!CanModerate(peer, out var player)) return false;

        var data = Load();
        data.BanName(player.Name);

        string address = NetworkManager.Instance.GetPeerAddress(peer);
        if (IPAddress.TryParse(address, out var ip) && !IPAddress.IsLoopback(ip)) data.BanAddress(player.Name, address);

        Save();
        NetworkManager.Instance.Kick(peer, BannedReason);
        ChatService.Broadcast($"{player.Name} was banned", NoticeColor);
        return true;
    }

    public static bool BanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        if (PlayerRegistry.TryGetByName(name, out var player)) return Ban(player.Peer);

        if (Load().BanName(name)) Save();
        return true;
    }

    public static bool Unban(string name)
    {
        bool removed = Load().UnbanName(name);
        if (removed) Save();
        return removed;
    }

    public static ModerationData Snapshot()
    {
        return Load().Copy();
    }

    public static bool UnbanAddress(string address)
    {
        bool removed = Load().UnbanAddress(address);
        if (removed) Save();
        return removed;
    }

    public static void SetMuted(string name, bool muted)
    {
        if (!Load().SetMuted(name, muted)) return;

        Save();
        PlayerListNetwork.MarkDirty();
        ChatService.Broadcast(muted ? $"{name} was muted" : $"{name} was unmuted", NoticeColor);
    }

    public static void Reset()
    {
        _data = new ModerationData();
        _loadedFolder = null;
    }

    private static bool CanModerate(int peer, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ConnectedPlayer? player)
    {
        player = null;
        var network = NetworkManager.Instance;
        return network.IsServer && peer != network.LocalPeerId && PlayerRegistry.TryGet(peer, out player);
    }

    private static ModerationData Load()
    {
        string? folder = World.WorldData?.WorldFolderPath;
        if (string.IsNullOrEmpty(folder)) folder = ModerationFolder.Resolve(folder, ConfiguredWorldFolder());
        if (folder == _loadedFolder) return _data;

        _loadedFolder = folder;
        _data = new ModerationData();

        string? path = GetPath(folder);
        if (path == null || !File.Exists(path)) return _data;

        try
        {
            _data = JsonSerializer.Deserialize<ModerationData>(File.ReadAllText(path)) ?? new ModerationData();
            _data.ApplyMissingDefaults();
        }
        catch (Exception ex)
        {
            Debug.Error($"[Moderation] Failed to read {path}: {ex.Message}");
        }

        return _data;
    }

    private static void Save()
    {
        string? path = GetPath(_loadedFolder);
        if (GameSession.IsRemoteClient) return;
        if (path == null)
        {
            Debug.Error("[Moderation] No world folder is known, the change is kept only until the server stops");
            return;
        }

        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Debug.Error($"[Moderation] Failed to save {path}: {ex.Message}");
        }
    }

    private static string? GetPath(string? folder)
    {
        return string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, FileName);
    }

    private static string? ConfiguredWorldFolder()
    {
        var config = GameSession.IsDedicated ? Dedicated.DedicatedServer.Config : null;
        return config == null ? null : ModerationFolder.DedicatedWorld(Spacebox.Game.GUI.WorldProvisioner.WorldsDirectory, config.WorldName);
    }
}
