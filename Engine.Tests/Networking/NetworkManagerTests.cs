using System.Net;
using Engine.Networking;

namespace Engine.Tests.Networking;

[Collection(GlobalStateCollection.Name)]
public class NetworkManagerTests
{
    private sealed class FakeTransport : INetTransport
    {
        public event Action<int> PeerConnected;
        public event Action<int, string> PeerDisconnected;
        public event Action<int, byte[], int, int> DataReceived;
        public event Action<string> ConnectionFailed;
        public event Action<IPEndPoint, byte[], int, int, bool> UnconnectedReceived;

        public bool IsRunning { get; private set; }
        public int MaxPeers { get; set; }

        public bool StartServer(int port, int maxPeers, string connectionKey)
        {
            MaxPeers = maxPeers;
            IsRunning = true;
            return true;
        }

        public bool Connect(string host, int port, string connectionKey) => true;
        public void Send(int peer, byte[] data, int offset, int length, DeliveryMode mode) { }
        public void SendUnconnected(IPEndPoint endPoint, byte[] data, int offset, int length) { }
        public void Disconnect(int peer, string reason) { }
        public int GetRoundTripTime(int peer) => 0;
        public string GetAddress(int peer) => string.Empty;
        public void Poll() { }
        public void Stop() => IsRunning = false;
        public void Dispose() => Stop();
    }

    [Fact]
    public void MaxPeers_ChangedWhileRunning_ReachesTheTransport()
    {
        var network = NetworkManager.Instance;
        var previousFactory = network.TransportFactory;
        int previousMax = network.MaxPeers;
        var transport = new FakeTransport();

        try
        {
            network.TransportFactory = () => transport;
            network.MaxPeers = 16;
            Assert.True(network.StartDedicated(47999));
            Assert.Equal(16, transport.MaxPeers);

            network.MaxPeers = 4;

            Assert.Equal(4, transport.MaxPeers);
        }
        finally
        {
            network.Shutdown();
            network.TransportFactory = previousFactory;
            network.MaxPeers = previousMax;
        }
    }
}
