using System.Net;
using Engine.Networking;

namespace Engine.Tests.Networking;

[Collection(GlobalStateCollection.Name)]
public class SendToPeersTests
{
    private struct PingMessage : INetMessage
    {
        public int Value;

        public void Serialize(NetWriter writer) => writer.Write(Value);

        public void Deserialize(NetReader reader) => Value = reader.ReadInt();
    }

    private sealed class RecordingTransport : INetTransport
    {
        public event Action<int> PeerConnected;
        public event Action<int, string> PeerDisconnected { add { } remove { } }
        public event Action<int, byte[], int, int> DataReceived { add { } remove { } }
        public event Action<string> ConnectionFailed { add { } remove { } }
        public event Action<IPEndPoint, byte[], int, int, bool> UnconnectedReceived { add { } remove { } }

        public List<int> SentTo { get; } = new List<int>();
        public bool IsRunning { get; private set; }
        public int MaxPeers { get; set; }

        public void Connected(int peer) => PeerConnected?.Invoke(peer);

        public bool StartServer(int port, int maxPeers, string connectionKey)
        {
            IsRunning = true;
            return true;
        }

        public bool Connect(string host, int port, string connectionKey) => true;
        public void Send(int peer, byte[] data, int offset, int length, DeliveryMode mode) => SentTo.Add(peer);
        public void SendUnconnected(IPEndPoint endPoint, byte[] data, int offset, int length) { }
        public void Disconnect(int peer, string reason) { }
        public int GetRoundTripTime(int peer) => 0;
        public string GetAddress(int peer) => string.Empty;
        public void Poll() { }
        public void Stop() => IsRunning = false;
        public void Dispose() => Stop();
    }

    [Fact]
    public void SendToPeers_ReachesOnlyTheListedConnectedPeers()
    {
        var network = NetworkManager.Instance;
        var previousFactory = network.TransportFactory;
        var transport = new RecordingTransport();

        try
        {
            network.TransportFactory = () => transport;
            Assert.True(network.StartDedicated(47998));
            transport.Connected(2);
            transport.Connected(3);
            transport.Connected(4);

            var peers = new List<int>();
            network.CopyAdmittedPeers(peers);
            peers.Sort();
            Assert.Equal(new[] { 2, 3, 4 }, peers);

            transport.SentTo.Clear();
            network.SendToPeers(new PingMessage { Value = 1 }, new List<int> { 2, 4, 9 }, DeliveryMode.Sequenced);

            Assert.Equal(new[] { 2, 4 }, transport.SentTo);
        }
        finally
        {
            network.Shutdown();
            network.TransportFactory = previousFactory;
        }
    }
}
