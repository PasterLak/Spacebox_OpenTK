namespace Engine.Networking;

public sealed class NetworkSpawner
{
    private sealed class Prefab
    {
        public Func<NetSpawnContext, Node3D> Create;
        public Action<Node3D> Destroy;
    }

    private readonly NetworkManager _network;
    private readonly Dictionary<string, Prefab> _prefabs = new Dictionary<string, Prefab>();
    private readonly Dictionary<uint, NetworkObject> _objects = new Dictionary<uint, NetworkObject>();
    private readonly List<NetworkObject> _buffer = new List<NetworkObject>();
    private readonly NetWriter _writer = new NetWriter();
    private readonly NetReader _reader = new NetReader();
    private uint _nextNetId = 1;

    public event Action<NetworkObject> Spawned;
    public event Action<NetworkObject> Despawned;

    public Dictionary<uint, NetworkObject>.ValueCollection Objects => _objects.Values;

    internal NetworkSpawner(NetworkManager network)
    {
        _network = network;
        RegisterHandlers();
    }

    internal void RegisterHandlers()
    {
        _network.Messages.Register<SpawnObjectMessage>(OnSpawnMessage);
        _network.Messages.Register<DespawnObjectMessage>(OnDespawnMessage);
        _network.Messages.Register<OwnershipMessage>(OnOwnershipMessage);
    }

    public void Register(string prefabKey, Func<NetSpawnContext, Node3D> create, Action<Node3D> destroy = null)
    {
        _prefabs[prefabKey] = new Prefab { Create = create, Destroy = destroy ?? DefaultDestroy };
    }

    public void Unregister(string prefabKey)
    {
        _prefabs.Remove(prefabKey);
    }

    public bool TryGet(uint netId, out NetworkObject networkObject)
    {
        return _objects.TryGetValue(netId, out networkObject);
    }

    public NetworkObject Spawn(string prefabKey, int ownerPeer, Action<NetWriter> writeSpawnData = null)
    {
        if (!_network.IsServer)
        {
            Debug.Error($"[NetworkSpawner] Only the server can spawn '{prefabKey}'");
            return null;
        }

        _writer.Reset();
        writeSpawnData?.Invoke(_writer);
        var spawnData = _writer.ToArray();

        var networkObject = Create(prefabKey, _nextNetId++, ownerPeer, spawnData, null);
        if (networkObject == null) return null;

        _network.SendToClients(BuildSpawnMessage(networkObject), DeliveryMode.Reliable);
        return networkObject;
    }

    public void Despawn(NetworkObject networkObject)
    {
        if (networkObject == null || !_network.IsServer) return;

        uint netId = networkObject.NetId;
        if (!_objects.ContainsKey(netId)) return;

        Remove(networkObject);
        _network.SendToClients(new DespawnObjectMessage { NetId = netId }, DeliveryMode.Reliable);
    }

    public void DespawnOwnedBy(int peer)
    {
        _buffer.Clear();
        foreach (var networkObject in _objects.Values)
        {
            if (networkObject.OwnerPeer == peer) _buffer.Add(networkObject);
        }

        for (int i = 0; i < _buffer.Count; i++) Despawn(_buffer[i]);
        _buffer.Clear();
    }

    public void SetOwner(NetworkObject networkObject, int ownerPeer)
    {
        if (!_network.IsServer || networkObject == null) return;

        ApplyOwner(networkObject, ownerPeer);
        _network.SendToClients(new OwnershipMessage { NetId = networkObject.NetId, OwnerPeer = ownerPeer }, DeliveryMode.Reliable);
    }

    public void Clear()
    {
        _buffer.Clear();
        _buffer.AddRange(_objects.Values);
        for (int i = 0; i < _buffer.Count; i++) Remove(_buffer[i]);
        _buffer.Clear();
        _objects.Clear();
        _nextNetId = 1;
    }

    internal void SendAllTo(int peer)
    {
        foreach (var networkObject in _objects.Values)
            _network.Send(peer, BuildSpawnMessage(networkObject), DeliveryMode.Reliable);
    }

    private SpawnObjectMessage BuildSpawnMessage(NetworkObject networkObject)
    {
        _writer.Reset();
        networkObject.WriteState(_writer);

        return new SpawnObjectMessage
        {
            NetId = networkObject.NetId,
            OwnerPeer = networkObject.OwnerPeer,
            PrefabKey = networkObject.PrefabKey,
            SpawnData = networkObject.SpawnData,
            State = _writer.ToArray()
        };
    }

    private NetworkObject Create(string prefabKey, uint netId, int ownerPeer, byte[] spawnData, byte[] state)
    {
        if (!_prefabs.TryGetValue(prefabKey, out var prefab))
        {
            Debug.Error($"[NetworkSpawner] Prefab '{prefabKey}' is not registered");
            return null;
        }

        _reader.Set(spawnData, 0, spawnData.Length);
        var node = prefab.Create(new NetSpawnContext(prefabKey, netId, ownerPeer, _reader));
        if (node == null) return null;

        var networkObject = FindNetworkObject(node) ?? node.AttachComponent(new NetworkObject());
        networkObject.NetId = netId;
        networkObject.OwnerPeer = ownerPeer;
        networkObject.PrefabKey = prefabKey;
        networkObject.SpawnData = spawnData;
        networkObject.CollectBehaviours();

        if (state != null && state.Length > 0)
        {
            _reader.Set(state, 0, state.Length);
            networkObject.ReadState(_reader);
        }

        networkObject.IsSpawned = true;
        _objects[netId] = networkObject;

        var behaviours = networkObject.Behaviours;
        for (int i = 0; i < behaviours.Count; i++) behaviours[i].OnNetworkSpawn();

        Spawned?.Invoke(networkObject);
        return networkObject;
    }

    private void Remove(NetworkObject networkObject)
    {
        _objects.Remove(networkObject.NetId);

        var behaviours = networkObject.Behaviours;
        for (int i = 0; i < behaviours.Count; i++) behaviours[i].OnNetworkDespawn();

        networkObject.IsSpawned = false;
        Despawned?.Invoke(networkObject);

        var node = networkObject.Owner;
        if (node != null && _prefabs.TryGetValue(networkObject.PrefabKey, out var prefab))
            prefab.Destroy(node);
    }

    private void ApplyOwner(NetworkObject networkObject, int ownerPeer)
    {
        int previous = networkObject.OwnerPeer;
        if (previous == ownerPeer) return;

        networkObject.OwnerPeer = ownerPeer;
        var behaviours = networkObject.Behaviours;
        for (int i = 0; i < behaviours.Count; i++) behaviours[i].OnOwnershipChanged(previous);
    }

    private void OnSpawnMessage(int sender, SpawnObjectMessage message)
    {
        if (_network.IsServer || _objects.ContainsKey(message.NetId)) return;
        Create(message.PrefabKey, message.NetId, message.OwnerPeer, message.SpawnData, message.State);
    }

    private void OnDespawnMessage(int sender, DespawnObjectMessage message)
    {
        if (_network.IsServer) return;
        if (_objects.TryGetValue(message.NetId, out var networkObject)) Remove(networkObject);
    }

    private void OnOwnershipMessage(int sender, OwnershipMessage message)
    {
        if (_network.IsServer) return;
        if (_objects.TryGetValue(message.NetId, out var networkObject)) ApplyOwner(networkObject, message.OwnerPeer);
    }

    private static NetworkObject FindNetworkObject(Node3D node)
    {
        var components = node.Components;
        for (int i = 0; i < components.Count; i++)
        {
            if (components[i] is NetworkObject networkObject) return networkObject;
        }
        return null;
    }

    private static void DefaultDestroy(Node3D node)
    {
        node.Destroy();
    }
}
