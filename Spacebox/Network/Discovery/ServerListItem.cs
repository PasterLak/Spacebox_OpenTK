using System.Net;

namespace Spacebox.Network.Discovery;

public sealed class ServerListItem
{
    public ServerListItem(string key, ServerEntry entry, bool isSaved)
    {
        Key = key;
        Entry = entry;
        IsSaved = isSaved;
        IconHash = ServerIconHash.Parse(entry.IconHash);
    }

    public string Key { get; }
    public ServerEntry Entry { get; }
    public bool IsSaved { get; }
    public ServerOnlineState State { get; internal set; }
    public ServerStatus Status { get; internal set; }
    public int PingMs { get; internal set; }
    public IPEndPoint EndPoint { get; internal set; }
    public ulong IconHash { get; internal set; }
    public double LastSeen { get; internal set; }

    public bool IsOnline => State == ServerOnlineState.Online && Status != null;
    public string DisplayName => string.IsNullOrWhiteSpace(Entry.Name) ? Status?.Name ?? Entry.Address : Entry.Name;
    public string Description => Status?.Description ?? Entry.Description ?? "";
    public bool IsCompatible => Status == null || Status.GameVersion == Engine.Application.Version;
}
