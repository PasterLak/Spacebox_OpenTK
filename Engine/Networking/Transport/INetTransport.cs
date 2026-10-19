using System.Net;

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
    event Action<IPEndPoint, byte[], int, int, bool> UnconnectedReceived;

    bool IsRunning { get; }

    bool StartServer(int port, int maxPeers, string connectionKey);
    bool Connect(string host, int port, string connectionKey);
    void Send(int peer, byte[] data, int offset, int length, DeliveryMode mode);
    void SendUnconnected(IPEndPoint endPoint, byte[] data, int offset, int length);
    void Disconnect(int peer, string reason);
    int GetRoundTripTime(int peer);
    string GetAddress(int peer);
    void Poll();
    void Stop();
}
