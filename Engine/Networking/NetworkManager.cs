namespace Engine.Networking;

public sealed class NetworkManager
{
    public const int ServerPeerId = 1;
    public const int DefaultPort = 7777;

    public static NetworkManager Instance { get; } = new NetworkManager();

    private readonly HashSet<int> _peers = new HashSet<int>();
    private readonly NetWriter _writer = new NetWriter(1024);
    private readonly NetReader _reader = new NetReader();
    private INetTransport _transport;
    private float _tickTimer;

    public NetworkMode Mode { get; private set; } = NetworkMode.None;
    public int LocalPeerId { get; private set; } = ServerPeerId;
    public string ConnectionKey { get; set; } = "Engine:" + Application.EngineVersion;
    public int MaxPeers { get; set; } = 16;
    public float TickRate { get; set; } = 20f;
    public Func<INetTransport> TransportFactory { get; set; } = () => new LiteNetTransport();

    public NetMessageRegistry Messages { get; } = new NetMessageRegistry();
    public NetworkSpawner Spawner { get; }
    public RpcSystem Rpcs { get; }
    public ReplicationSystem Replication { get; }
    public NetTransferService Transfers { get; }

    public bool IsActive => Mode != NetworkMode.None;
    public bool IsServer => Mode == NetworkMode.Offline || Mode == NetworkMode.Host || Mode == NetworkMode.DedicatedServer;
    public bool IsClient => Mode == NetworkMode.Offline || Mode == NetworkMode.Host || Mode == NetworkMode.Client;
    public bool IsRemoteClient => Mode == NetworkMode.Client;
    public bool IsDedicated => Mode == NetworkMode.DedicatedServer;
    public bool IsListening => IsServer && _transport != null && _transport.IsRunning;
    public bool IsConnected => Mode == NetworkMode.Client && _peers.Contains(ServerPeerId);
    public IReadOnlyCollection<int> Peers => _peers;

    public event Action<int> PeerConnected;
    public event Action<int, string> PeerDisconnected;
    public event Action ConnectedToServer;
    public event Action<string> DisconnectedFromServer;
    public event Action<string> ConnectionFailed;
    public event Action<NetworkMode> Started;
    public event Action Stopped;

    private NetworkManager()
    {
        Spawner = new NetworkSpawner(this);
        Rpcs = new RpcSystem(this);
        Replication = new ReplicationSystem(this);
        Transfers = new NetTransferService(this);
        Messages.Register<AssignPeerIdMessage>(OnAssignPeerId);
    }

    public void StartOffline()
    {
        Shutdown();
        LocalPeerId = ServerPeerId;
        SetMode(NetworkMode.Offline);
    }

    public bool StartHost(int port)
    {
        if (Mode != NetworkMode.Offline) StartOffline();
        return StartListening(port);
    }

    public bool StartListening(int port)
    {
        if (Mode != NetworkMode.Offline && Mode != NetworkMode.Host) return false;
        if (IsListening) return true;

        if (!CreateTransport().StartServer(port, MaxPeers, ConnectionKey))
        {
            DestroyTransport();
            return false;
        }

        Mode = NetworkMode.Host;
        Debug.Success($"[Network] Hosting on port {port}");
        return true;
    }

    public void StopListening()
    {
        if (Mode != NetworkMode.Host) return;

        DisconnectAllPeers("Server closed");
        DestroyTransport();
        Mode = NetworkMode.Offline;
    }

    public bool StartDedicated(int port)
    {
        Shutdown();
        LocalPeerId = ServerPeerId;

        if (!CreateTransport().StartServer(port, MaxPeers, ConnectionKey))
        {
            DestroyTransport();
            return false;
        }

        SetMode(NetworkMode.DedicatedServer);
        Debug.Success($"[Network] Dedicated server on port {port}");
        return true;
    }

    public bool Connect(string host, int port)
    {
        Shutdown();
        LocalPeerId = 0;

        if (!CreateTransport().Connect(host, port, ConnectionKey))
        {
            DestroyTransport();
            return false;
        }

        SetMode(NetworkMode.Client);
        return true;
    }

    public void Shutdown(string reason = "Shutdown")
    {
        if (Mode == NetworkMode.None) return;

        DisconnectAllPeers(reason);
        _transport?.Poll();
        DestroyTransport();
        Spawner.Clear();
        _peers.Clear();
        Mode = NetworkMode.None;
        LocalPeerId = ServerPeerId;
        Stopped?.Invoke();
    }

    public float GetOneWayLatency(int peer)
    {
        if (peer == LocalPeerId || _transport == null) return 0f;
        return _transport.GetRoundTripTime(peer) * 0.0005f;
    }

    public float LatencyToServer => IsServer ? 0f : GetOneWayLatency(ServerPeerId);

    public void Kick(int peer, string reason)
    {
        if (!IsServer || peer == LocalPeerId) return;
        _transport?.Disconnect(peer, reason);
    }

    public void Update()
    {
        if (Mode == NetworkMode.None) return;

        _transport?.Poll();
        Transfers.Update();

        _tickTimer += Time.Delta;
        float interval = 1f / MathF.Max(1f, TickRate);
        if (_tickTimer < interval) return;

        _tickTimer %= interval;
        Replication.Tick();
    }

    public void Send<T>(int peer, T message, DeliveryMode mode = DeliveryMode.Reliable) where T : INetMessage
    {
        if (peer == LocalPeerId)
        {
            Messages.DispatchLocal(LocalPeerId, message);
            return;
        }

        if (_transport == null) return;

        _writer.Reset();
        Messages.Write(_writer, message);
        _transport.Send(peer, _writer.Buffer, 0, _writer.Length, mode);
    }

    public void SendToServer<T>(T message, DeliveryMode mode = DeliveryMode.Reliable) where T : INetMessage
    {
        if (IsServer)
        {
            Messages.DispatchLocal(LocalPeerId, message);
            return;
        }

        Send(ServerPeerId, message, mode);
    }

    public void SendToClients<T>(T message, DeliveryMode mode = DeliveryMode.Reliable, int exceptPeer = 0) where T : INetMessage
    {
        if (!IsServer || _transport == null || _peers.Count == 0) return;

        _writer.Reset();
        Messages.Write(_writer, message);

        foreach (var peer in _peers)
        {
            if (peer != exceptPeer) _transport.Send(peer, _writer.Buffer, 0, _writer.Length, mode);
        }
    }

    private void SetMode(NetworkMode mode)
    {
        Mode = mode;
        _tickTimer = 0f;
        Started?.Invoke(mode);
    }

    private INetTransport CreateTransport()
    {
        DestroyTransport();
        _transport = TransportFactory();
        _transport.PeerConnected += OnPeerConnected;
        _transport.PeerDisconnected += OnPeerDisconnected;
        _transport.DataReceived += OnDataReceived;
        _transport.ConnectionFailed += OnConnectionFailed;
        return _transport;
    }

    private void DestroyTransport()
    {
        if (_transport == null) return;

        _transport.PeerConnected -= OnPeerConnected;
        _transport.PeerDisconnected -= OnPeerDisconnected;
        _transport.DataReceived -= OnDataReceived;
        _transport.ConnectionFailed -= OnConnectionFailed;
        _transport.Dispose();
        _transport = null;
    }

    private void DisconnectAllPeers(string reason)
    {
        if (_transport == null) return;

        foreach (var peer in _peers) _transport.Disconnect(peer, reason);
    }

    private void OnPeerConnected(int peer)
    {
        _peers.Add(peer);

        if (IsServer)
        {
            Send(peer, new AssignPeerIdMessage { PeerId = peer });
            PeerConnected?.Invoke(peer);
        }
    }

    private void OnPeerDisconnected(int peer, string reason)
    {
        _peers.Remove(peer);

        if (IsServer)
        {
            PeerDisconnected?.Invoke(peer, reason);
            return;
        }

        var mode = Mode;
        Shutdown(reason);
        if (mode == NetworkMode.Client) DisconnectedFromServer?.Invoke(reason);
    }

    private void OnConnectionFailed(string reason)
    {
        Shutdown(reason);
        ConnectionFailed?.Invoke(reason);
    }

    private void OnDataReceived(int peer, byte[] data, int offset, int length)
    {
        _reader.Set(data, offset, length);

        try
        {
            Messages.Dispatch(peer, _reader);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Network] Failed to handle message from peer {peer}: {ex}");
        }
    }

    private void OnAssignPeerId(int sender, AssignPeerIdMessage message)
    {
        if (IsServer) return;

        LocalPeerId = message.PeerId;
        ConnectedToServer?.Invoke();
    }

    public void SendSpawnedObjectsTo(int peer)
    {
        if (IsServer) Spawner.SendAllTo(peer);
    }
}
