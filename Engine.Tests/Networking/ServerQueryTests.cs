using System.Net;
using Engine.Networking;
using Engine.Networking.Query;

namespace Engine.Tests.Networking;

public class ServerQueryTests
{
    private static readonly IPEndPoint ServerA = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 7777);
    private static readonly IPEndPoint ServerB = new IPEndPoint(IPAddress.Parse("10.0.0.2"), 7777);
    private static readonly IPEndPoint Client = new IPEndPoint(IPAddress.Parse("10.0.0.9"), 50000);

    private double _now;

    private ServerQueryClient CreateClient(FakeQueryNetwork network, out FakeQueryNetwork.Socket socket)
    {
        socket = network.CreateClient(Client);
        return new ServerQueryClient(socket, () => _now);
    }

    private static ServerQueryResponder CreateResponder(byte[] info, byte[] blob = null)
    {
        return new ServerQueryResponder { InfoProvider = () => info, BlobProvider = () => blob };
    }

    [Fact]
    public void RequestInfo_ReturnsPayloadKeyAndRoundTrip()
    {
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, CreateResponder(new byte[] { 1, 2, 3 }));
        var client = CreateClient(network, out _);
        ServerQueryResult result = null;
        client.InfoReceived += r => result = r;

        client.RequestInfo("a", "10.0.0.1", 7777);
        _now = 0.05;
        client.Update();

        Assert.NotNull(result);
        Assert.Equal("a", result.Key);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Info);
        Assert.Equal(50, result.RoundTripMs);
        Assert.False(result.FromBroadcast);
        Assert.Equal(ServerA, result.EndPoint);
    }

    [Fact]
    public void RequestInfo_WithoutAnswer_TimesOut()
    {
        var network = new FakeQueryNetwork();
        var client = CreateClient(network, out _);
        string timedOut = null;
        client.InfoTimedOut += key => timedOut = key;

        client.RequestInfo("missing", "10.0.0.5", 7777);
        client.Update();
        Assert.Null(timedOut);

        _now = client.Timeout + 0.1;
        client.Update();

        Assert.Equal("missing", timedOut);
    }

    [Fact]
    public void RequestInfo_InvalidPort_TimesOutImmediately()
    {
        var client = CreateClient(new FakeQueryNetwork(), out _);
        string timedOut = null;
        client.InfoTimedOut += key => timedOut = key;

        client.RequestInfo("bad", "10.0.0.1", 0);

        Assert.Equal("bad", timedOut);
    }

    [Fact]
    public void Discover_ReceivesEveryServerOnPort()
    {
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, CreateResponder(new byte[] { 1 }));
        network.AddServer(ServerB, CreateResponder(new byte[] { 2 }));
        var client = CreateClient(network, out _);
        var results = new List<ServerQueryResult>();
        client.InfoReceived += results.Add;

        client.Discover(7777);
        client.Update();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.FromBroadcast));
        Assert.All(results, r => Assert.Null(r.Key));
    }

    [Fact]
    public void Discover_IgnoredWhenBroadcastsDisabled()
    {
        var network = new FakeQueryNetwork();
        var responder = CreateResponder(new byte[] { 1 });
        responder.AnswerBroadcasts = false;
        network.AddServer(ServerA, responder);
        var client = CreateClient(network, out _);
        int received = 0;
        client.InfoReceived += _ => received++;

        client.Discover(7777);
        client.Update();

        Assert.Equal(0, received);
    }

    [Fact]
    public void Discover_SendsDirectedBroadcastsWithSameRequest()
    {
        var directed = new IPEndPoint(IPAddress.Parse("10.0.0.255"), 7777);
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, CreateResponder(new byte[] { 1 }));
        network.AddServer(directed, CreateResponder(new byte[] { 2 }));
        var client = CreateClient(network, out var socket);
        var results = new List<ServerQueryResult>();
        client.InfoReceived += results.Add;

        client.Discover(7777, new[] { directed.Address });
        client.Update();

        Assert.Equal(2, socket.SentCount);
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.FromBroadcast));
    }

    [Fact]
    public void Discover_Timeout_DoesNotReportOffline()
    {
        var client = CreateClient(new FakeQueryNetwork(), out _);
        int timedOut = 0;
        client.InfoTimedOut += _ => timedOut++;

        client.Discover(7777);
        _now = 10;
        client.Update();

        Assert.Equal(0, timedOut);
    }

    [Fact]
    public void Responder_WithoutProvider_DoesNotAnswer()
    {
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, new ServerQueryResponder());
        var client = CreateClient(network, out _);
        int received = 0;
        client.InfoReceived += _ => received++;

        client.RequestInfo("a", "10.0.0.1", 7777);
        client.Update();

        Assert.Equal(0, received);
    }

    [Fact]
    public void Info_ReportsBlobHashAndLength()
    {
        var blob = CreateBlob(2500);
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, CreateResponder(new byte[] { 1 }, blob));
        var client = CreateClient(network, out _);
        ServerQueryResult result = null;
        client.InfoReceived += r => result = r;

        client.RequestInfo("a", "10.0.0.1", 7777);
        client.Update();

        Assert.Equal(ServerQueryProtocol.ComputeHash(blob), result.BlobHash);
        Assert.Equal(blob.Length, result.BlobLength);
    }

    [Fact]
    public void RequestBlob_ReassemblesChunks()
    {
        var blob = CreateBlob(ServerQueryProtocol.ChunkSize * 2 + 100);
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, CreateResponder(new byte[] { 1 }, blob));
        var client = CreateClient(network, out _);
        byte[] received = null;
        client.BlobReceived += (_, data) => received = data;

        client.RequestBlob(ServerA, ServerQueryProtocol.ComputeHash(blob), blob.Length);
        client.Update();

        Assert.Equal(blob, received);
    }

    [Fact]
    public void RequestBlob_RetriesLostChunks()
    {
        var blob = CreateBlob(ServerQueryProtocol.ChunkSize * 3);
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, CreateResponder(new byte[] { 1 }, blob));
        bool dropFirst = true;
        network.DropServerPacket = (data, offset, length) =>
        {
            if (!dropFirst || data[offset + 4] != (byte)ServerQueryProtocol.PacketType.BlobChunk) return false;
            dropFirst = false;
            return true;
        };
        var client = CreateClient(network, out _);
        byte[] received = null;
        client.BlobReceived += (_, data) => received = data;

        client.RequestBlob(ServerA, ServerQueryProtocol.ComputeHash(blob), blob.Length);
        client.Update();
        Assert.Null(received);

        _now = client.Timeout + 0.1;
        client.Update();
        client.Update();

        Assert.Equal(blob, received);
    }

    [Fact]
    public void RequestBlob_WithWrongHash_IsNotAnswered()
    {
        var blob = CreateBlob(100);
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, CreateResponder(new byte[] { 1 }, blob));
        var client = CreateClient(network, out _);
        int received = 0;
        client.BlobReceived += (_, _) => received++;

        client.RequestBlob(ServerA, 12345, blob.Length);
        client.Update();

        Assert.Equal(0, received);
    }

    [Fact]
    public void Responder_IgnoresForeignPackets()
    {
        var responder = CreateResponder(new byte[] { 1 });
        int sent = 0;

        bool handled = responder.Handle(ServerA, new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6, false, (_, _, _, _) => sent++);

        Assert.False(handled);
        Assert.Equal(0, sent);
    }

    [Fact]
    public void Responder_IgnoresTruncatedRequest()
    {
        var responder = CreateResponder(new byte[] { 1 });
        var writer = new NetWriter();
        ServerQueryProtocol.WriteHeader(writer, ServerQueryProtocol.PacketType.InfoRequest);
        int sent = 0;

        bool handled = responder.Handle(ServerA, writer.ToArray(), 0, writer.Length, false, (_, _, _, _) => sent++);

        Assert.False(handled);
        Assert.Equal(0, sent);
    }

    [Fact]
    public void Responder_SkipsOversizedInfo()
    {
        var network = new FakeQueryNetwork();
        network.AddServer(ServerA, CreateResponder(new byte[ServerQueryProtocol.MaxInfoSize + 1]));
        var client = CreateClient(network, out _);
        int received = 0;
        client.InfoReceived += _ => received++;

        client.RequestInfo("a", "10.0.0.1", 7777);
        client.Update();

        Assert.Equal(0, received);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(1024, 1)]
    [InlineData(1025, 2)]
    public void ChunkCount_RoundsUp(int length, int expected)
    {
        Assert.Equal(expected, ServerQueryProtocol.GetChunkCount(length));
    }

    [Fact]
    public void ComputeHash_DiffersForDifferentData()
    {
        Assert.NotEqual(ServerQueryProtocol.ComputeHash(new byte[] { 1 }), ServerQueryProtocol.ComputeHash(new byte[] { 2 }));
        Assert.Equal(ServerQueryProtocol.ComputeHash(new byte[] { 3, 4 }), ServerQueryProtocol.ComputeHash(new byte[] { 3, 4 }));
    }

    private static byte[] CreateBlob(int length)
    {
        var data = new byte[length];
        for (int i = 0; i < length; i++) data[i] = (byte)(i * 31 + 7);
        return data;
    }
}
