using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Engine.Networking;

public sealed class LiteNetTransport : INetTransport
{
    private const int FirstClientPeerId = 2;

    private readonly EventBasedNetListener _listener = new EventBasedNetListener();
    private readonly Dictionary<int, NetPeer> _peers = new Dictionary<int, NetPeer>();
    private readonly NetDataWriter _reasonWriter = new NetDataWriter();
    private NetManager _manager;
    private string _connectionKey;
    private int _maxPeers;
    private bool _isServer;
    private bool _connectedToServer;

    public event Action<int> PeerConnected;
    public event Action<int, string> PeerDisconnected;
    public event Action<int, byte[], int, int> DataReceived;
    public event Action<string> ConnectionFailed;

    public bool IsRunning => _manager != null && _manager.IsRunning;

    public LiteNetTransport()
    {
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;
        _listener.NetworkErrorEvent += OnNetworkError;
    }

    public bool StartServer(int port, int maxPeers, string connectionKey)
    {
        Stop();
        _isServer = true;
        _connectionKey = connectionKey;
        _maxPeers = maxPeers;
        _manager = CreateManager();

        if (_manager.Start(port)) return true;

        Debug.Error($"[LiteNetTransport] Failed to start server on port {port}");
        _manager = null;
        return false;
    }

    public bool Connect(string host, int port, string connectionKey)
    {
        Stop();
        _isServer = false;
        _connectedToServer = false;
        _manager = CreateManager();

        if (!_manager.Start())
        {
            Debug.Error("[LiteNetTransport] Failed to start client socket");
            _manager = null;
            return false;
        }

        if (_manager.Connect(host, port, connectionKey) != null) return true;

        Debug.Error($"[LiteNetTransport] Failed to connect to {host}:{port}");
        Stop();
        return false;
    }

    public void Send(int peer, byte[] data, int offset, int length, DeliveryMode mode)
    {
        if (_peers.TryGetValue(peer, out var netPeer))
            netPeer.Send(data, offset, length, ToDeliveryMethod(mode));
    }

    public void Disconnect(int peer, string reason)
    {
        if (!_peers.TryGetValue(peer, out var netPeer)) return;

        _reasonWriter.Reset();
        _reasonWriter.Put(reason ?? string.Empty);
        netPeer.Disconnect(_reasonWriter);
    }

    public void Poll()
    {
        _manager?.PollEvents();
    }

    public void Stop()
    {
        if (_manager == null) return;

        _manager.Stop(true);
        _manager = null;
        _peers.Clear();
        _connectedToServer = false;
    }

    public void Dispose()
    {
        Stop();
    }

    private NetManager CreateManager()
    {
        return new NetManager(_listener)
        {
            AutoRecycle = true,
            UnconnectedMessagesEnabled = false,
            DisconnectTimeout = 10000,
            ChannelsCount = 1
        };
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
        if (!_isServer || _peers.Count >= _maxPeers)
        {
            request.Reject();
            return;
        }

        request.AcceptIfKey(_connectionKey);
    }

    private void OnPeerConnected(NetPeer netPeer)
    {
        int peer = _isServer ? netPeer.Id + FirstClientPeerId : NetworkManager.ServerPeerId;
        _peers[peer] = netPeer;
        _connectedToServer |= !_isServer;
        PeerConnected?.Invoke(peer);
    }

    private void OnPeerDisconnected(NetPeer netPeer, DisconnectInfo info)
    {
        int peer = _isServer ? netPeer.Id + FirstClientPeerId : NetworkManager.ServerPeerId;
        string reason = ReadReason(info);

        if (!_isServer && !_connectedToServer)
        {
            ConnectionFailed?.Invoke(reason);
            return;
        }

        _peers.Remove(peer);
        PeerDisconnected?.Invoke(peer, reason);
    }

    private void OnNetworkReceive(NetPeer netPeer, NetPacketReader reader, byte channel, DeliveryMethod method)
    {
        int peer = _isServer ? netPeer.Id + FirstClientPeerId : NetworkManager.ServerPeerId;
        DataReceived?.Invoke(peer, reader.RawData, reader.UserDataOffset, reader.UserDataSize);
    }

    private void OnNetworkError(IPEndPoint endPoint, SocketError error)
    {
        Debug.Error($"[LiteNetTransport] Socket error {error} ({endPoint})");
    }

    private static string ReadReason(DisconnectInfo info)
    {
        if (info.AdditionalData != null && info.AdditionalData.AvailableBytes > 0 && info.AdditionalData.TryGetString(out var text))
            return text;

        return info.Reason.ToString();
    }

    private static DeliveryMethod ToDeliveryMethod(DeliveryMode mode)
    {
        return mode switch
        {
            DeliveryMode.ReliableUnordered => DeliveryMethod.ReliableUnordered,
            DeliveryMode.Sequenced => DeliveryMethod.Sequenced,
            DeliveryMode.Unreliable => DeliveryMethod.Unreliable,
            _ => DeliveryMethod.ReliableOrdered
        };
    }
}
