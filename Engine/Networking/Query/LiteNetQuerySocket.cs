using System.Net;
using LiteNetLib;

namespace Engine.Networking.Query;

public sealed class LiteNetQuerySocket : IQuerySocket
{
    private readonly EventBasedNetListener _listener = new EventBasedNetListener();
    private NetManager _manager;

    public event Action<IPEndPoint, byte[], int, int> Received;

    public LiteNetQuerySocket()
    {
        _listener.NetworkReceiveUnconnectedEvent += OnReceiveUnconnected;
    }

    public bool Start()
    {
        if (_manager != null) return _manager.IsRunning;

        _manager = new NetManager(_listener)
        {
            AutoRecycle = true,
            UnconnectedMessagesEnabled = true
        };

        if (_manager.Start()) return true;

        Debug.Error("[ServerQuery] Failed to start query socket");
        _manager = null;
        return false;
    }

    public void Send(IPEndPoint endPoint, byte[] data, int offset, int length)
    {
        _manager?.SendUnconnectedMessage(data, offset, length, endPoint);
    }

    public void Broadcast(int port, byte[] data, int offset, int length)
    {
        _manager?.SendBroadcast(data, offset, length, port);
    }

    public void Poll()
    {
        _manager?.PollEvents();
    }

    public void Dispose()
    {
        _manager?.Stop();
        _manager = null;
    }

    private void OnReceiveUnconnected(IPEndPoint endPoint, NetPacketReader reader, UnconnectedMessageType type)
    {
        Received?.Invoke(endPoint, reader.RawData, reader.UserDataOffset, reader.UserDataSize);
    }
}
