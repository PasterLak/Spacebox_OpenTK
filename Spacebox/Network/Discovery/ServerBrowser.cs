using System.Net;
using Engine.Networking;
using Engine.Networking.Query;

namespace Spacebox.Network.Discovery;

public sealed class ServerBrowser : IDisposable
{
    public const float AutoRefreshInterval = 15f;
    public const float LocalServerLifetime = AutoRefreshInterval * 2f + 5f;

    private const string SavedKeyPrefix = "saved:";
    private const string LocalhostKey = "localhost";
    private const string KnownLocalKeyPrefix = "known:";

    private readonly ServerQueryClient _client;
    private readonly List<ServerListItem> _saved = new List<ServerListItem>();
    private readonly List<ServerListItem> _local = new List<ServerListItem>();
    private readonly Dictionary<string, ServerListItem> _savedByKey = new Dictionary<string, ServerListItem>();
    private readonly Dictionary<long, ServerListItem> _localByInstance = new Dictionary<long, ServerListItem>();
    private readonly List<long> _staleLocal = new List<long>();
    private int _nextSavedKey;
    private double _time;
    private double _nextRefresh;

    public IReadOnlyList<ServerListItem> Saved => _saved;
    public IReadOnlyList<ServerListItem> Local => _local;
    public int DiscoveryPort { get; set; } = NetworkManager.DefaultPort;
    public Func<ulong, bool> HasIcon { get; set; }
    public LocalServerHistory KnownLocal { get; set; }
    public IReadOnlyList<IPAddress> DirectedBroadcasts { get; set; }

    public event Action<ulong, byte[]> IconReceived;
    public event Action SavedEntriesChanged;
    public event Action KnownLocalChanged;

    public ServerBrowser(IQuerySocket socket, Func<double> clock = null)
    {
        _client = new ServerQueryClient(socket, clock);
        _client.InfoReceived += OnInfoReceived;
        _client.InfoTimedOut += OnInfoTimedOut;
        _client.BlobReceived += OnBlobReceived;
    }

    public void SetSavedServers(IReadOnlyList<ServerEntry> entries)
    {
        var previous = new Dictionary<ServerEntry, ServerListItem>();
        foreach (var item in _saved) previous[item.Entry] = item;

        _saved.Clear();
        _savedByKey.Clear();

        foreach (var entry in entries)
        {
            if (!previous.TryGetValue(entry, out var item))
                item = new ServerListItem(SavedKeyPrefix + _nextSavedKey++, entry, true);

            _saved.Add(item);
            _savedByKey[item.Key] = item;
        }
    }

    public void Refresh()
    {
        _nextRefresh = _time + AutoRefreshInterval;

        PingKnownLocal();

        foreach (var item in _saved)
            Ping(item);

        _client.Discover(DiscoveryPort, DirectedBroadcasts);
        _client.RequestInfo(LocalhostKey, IPAddress.Loopback.ToString(), DiscoveryPort);
    }

    public void Ping(ServerListItem item)
    {
        if (item == null) return;

        item.State = ServerOnlineState.Pinging;
        if (item.IsSaved) _client.RequestInfo(item.Key, item.Entry.IP, item.Entry.Port);
        else if (item.EndPoint != null) _client.RequestInfo(item.Key, item.EndPoint.Address.ToString(), item.EndPoint.Port);
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        _client.Update();

        if (_time >= _nextRefresh) Refresh();
        RemoveStaleLocalServers();
    }

    public bool IsHiddenLocal(ServerListItem local)
    {
        if (local?.Status == null) return false;

        foreach (var saved in _saved)
        {
            if (saved.IsOnline && saved.Status.InstanceId == local.Status.InstanceId) return true;
            if (string.Equals(saved.Entry.IP, local.Entry.IP, StringComparison.OrdinalIgnoreCase) && saved.Entry.Port == local.Entry.Port) return true;
        }
        return false;
    }

    public void Dispose()
    {
        _client.InfoReceived -= OnInfoReceived;
        _client.InfoTimedOut -= OnInfoTimedOut;
        _client.BlobReceived -= OnBlobReceived;
        _client.Dispose();
    }

    private void OnInfoReceived(ServerQueryResult result)
    {
        if (!ServerStatus.TryDeserialize(result.Info, out var status)) return;

        var item = result.Key != null && _savedByKey.TryGetValue(result.Key, out var saved)
            ? saved
            : UpsertLocal(result, status);

        if (item == null) return;

        item.State = ServerOnlineState.Online;
        item.Status = status;
        item.PingMs = result.RoundTripMs;
        item.EndPoint = result.EndPoint;
        item.LastSeen = _time;

        if (item.IsSaved) CacheSavedEntry(item, status, result.BlobHash);
        else
        {
            item.IconHash = result.BlobHash;
            if (KnownLocal != null && KnownLocal.Remember(result.EndPoint)) KnownLocalChanged?.Invoke();
        }

        RequestIcon(result);
    }

    private void PingKnownLocal()
    {
        if (KnownLocal == null) return;

        foreach (var address in KnownLocal.Addresses)
        {
            if (!LocalServerHistory.TryParse(address, out var endPoint)) continue;
            _client.RequestInfo(KnownLocalKeyPrefix + address, endPoint.Address.ToString(), endPoint.Port);
        }
    }

    private ServerListItem UpsertLocal(ServerQueryResult result, ServerStatus status)
    {
        if (status.InstanceId == 0) return null;

        if (_localByInstance.TryGetValue(status.InstanceId, out var existing)) return existing;

        var address = result.EndPoint.Address.IsIPv4MappedToIPv6 ? result.EndPoint.Address.MapToIPv4() : result.EndPoint.Address;
        var entry = new ServerEntry
        {
            Name = status.Name,
            Description = status.Description,
            IP = address.ToString(),
            Port = result.EndPoint.Port,
            IconHash = ServerIconHash.Format(result.BlobHash)
        };

        var item = new ServerListItem("local:" + status.InstanceId, entry, false);
        _localByInstance[status.InstanceId] = item;
        _local.Add(item);
        return item;
    }

    private void CacheSavedEntry(ServerListItem item, ServerStatus status, ulong iconHash)
    {
        bool changed = false;
        string description = status.Description ?? "";
        if (item.Entry.Description != description)
        {
            item.Entry.Description = description;
            changed = true;
        }

        string hash = ServerIconHash.Format(iconHash);
        if (item.Entry.IconHash != hash)
        {
            item.Entry.IconHash = hash;
            changed = true;
        }

        item.IconHash = iconHash;
        if (changed) SavedEntriesChanged?.Invoke();
    }

    private void RequestIcon(ServerQueryResult result)
    {
        if (result.BlobHash == 0 || result.BlobLength <= 0) return;
        if (HasIcon != null && HasIcon(result.BlobHash)) return;

        _client.RequestBlob(result.EndPoint, result.BlobHash, result.BlobLength);
    }

    private void OnInfoTimedOut(string key)
    {
        if (key == null || !_savedByKey.TryGetValue(key, out var item)) return;

        item.State = ServerOnlineState.Offline;
        item.Status = null;
        item.PingMs = 0;
    }

    private void OnBlobReceived(ulong hash, byte[] data)
    {
        if (!ServerIconFile.IsValid(data)) return;
        IconReceived?.Invoke(hash, data);
    }

    private void RemoveStaleLocalServers()
    {
        if (_local.Count == 0) return;

        _staleLocal.Clear();
        foreach (var pair in _localByInstance)
        {
            if (_time - pair.Value.LastSeen > LocalServerLifetime) _staleLocal.Add(pair.Key);
        }

        foreach (long instanceId in _staleLocal)
        {
            _local.Remove(_localByInstance[instanceId]);
            _localByInstance.Remove(instanceId);
        }
    }
}
