namespace Engine.Networking;

public enum DeliveryMode : byte
{
    Reliable,
    ReliableUnordered,
    Sequenced,
    Unreliable
}

public interface INetTransport : IDisposable
{
    event Action<int> PeerConnected;
    event Action<int, string> PeerDisconnected;
    event Action<int, byte[], int, int> DataReceived;
    event Action<string> ConnectionFailed;

    bool IsRunning { get; }

    bool StartServer(int port, int maxPeers, string connectionKey);
    bool Connect(string host, int port, string connectionKey);
    void Send(int peer, byte[] data, int offset, int length, DeliveryMode mode);
    void Disconnect(int peer, string reason);
    int GetRoundTripTime(int peer);
    void Poll();
    void Stop();
}
