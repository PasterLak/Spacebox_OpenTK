using System.Net;
using System.Text.Json;
using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Network.Moderation;

public sealed class ModerationData
{
    public List<string> BannedNames { get; set; } = new List<string>();
    public List<string> BannedAddresses { get; set; } = new List<string>();
    public List<string> MutedNames { get; set; } = new List<string>();
}

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
        var data = Load();
        return Contains(data.BannedNames, name) || (!string.IsNullOrEmpty(address) && Contains(data.BannedAddresses, address));
    }

    public static bool IsMuted(string name)
    {
        return Contains(Load().MutedNames, name);
    }

    public static void Kick(int peer)
    {
        if (!CanModerate(peer, out var player)) return;

        NetworkManager.Instance.Kick(peer, KickedReason);
        ChatService.Broadcast($"{player.Name} was kicked", NoticeColor);
    }

    public static void Ban(int peer)
    {
        if (!CanModerate(peer, out var player)) return;

        var data = Load();
        AddUnique(data.BannedNames, player.Name);

        string address = NetworkManager.Instance.GetPeerAddress(peer);
        if (IPAddress.TryParse(address, out var ip) && !IPAddress.IsLoopback(ip)) AddUnique(data.BannedAddresses, address);

        Save();
        NetworkManager.Instance.Kick(peer, BannedReason);
        ChatService.Broadcast($"{player.Name} was banned", NoticeColor);
    }

    public static void BanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        if (PlayerRegistry.TryGetByName(name, out var player))
        {
            Ban(player.Peer);
            return;
        }

        AddUnique(Load().BannedNames, name.Trim());
        Save();
    }

    public static bool Unban(string name)
    {
        var data = Load();
        bool removed = data.BannedNames.RemoveAll(entry => string.Equals(entry, name, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed) Save();
        return removed;
    }

    public static void SetMuted(string name, bool muted)
    {
        if (string.IsNullOrWhiteSpace(name) || IsMuted(name) == muted) return;

        var data = Load();
        if (muted) AddUnique(data.MutedNames, name);
        else data.MutedNames.RemoveAll(entry => string.Equals(entry, name, StringComparison.OrdinalIgnoreCase));

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
        if (folder == _loadedFolder) return _data;

        _loadedFolder = folder;
        _data = new ModerationData();

        string? path = GetPath(folder);
        if (path == null || !File.Exists(path)) return _data;

        try
        {
            _data = JsonSerializer.Deserialize<ModerationData>(File.ReadAllText(path)) ?? new ModerationData();
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
        if (path == null || GameSession.IsRemoteClient) return;

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

    private static bool Contains(List<string> list, string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], value, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static void AddUnique(List<string> list, string value)
    {
        if (!Contains(list, value)) list.Add(value);
    }
}
