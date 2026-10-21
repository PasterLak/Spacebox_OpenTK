using System.Net;
using Engine.Networking;
using Engine.Networking.Query;

namespace Engine.Tests.Networking;

public class NetworkHardeningTests
{
    private sealed class PingMessage : INetMessage
    {
        public int Value;
        public void Serialize(NetWriter writer) => writer.Write(Value);
        public void Deserialize(NetReader reader) => Value = reader.ReadInt();
    }

    private sealed class HelloMessage : INetMessage
    {
        public void Serialize(NetWriter writer) { }
        public void Deserialize(NetReader reader) { }
    }

    private static NetReader Encode<T>(NetMessageRegistry registry, T message) where T : INetMessage
    {
        var writer = new NetWriter();
        registry.Write(writer, message);
        return new NetReader(writer.ToArray());
    }

    [Fact]
    public void Dispatch_DropsMessagesFromPeersNotAdmitted()
    {
        var registry = new NetMessageRegistry();
        int pings = 0, hellos = 0;
        registry.Register<PingMessage>((_, _) => pings++);
        registry.Register<HelloMessage>((_, _) => hellos++);
        registry.AllowBeforeAdmission<HelloMessage>();

        Assert.False(registry.Dispatch(5, Encode(registry, new PingMessage { Value = 1 }), admitted: false));
        Assert.True(registry.Dispatch(5, Encode(registry, new HelloMessage()), admitted: false));
        Assert.True(registry.Dispatch(5, Encode(registry, new PingMessage { Value = 1 }), admitted: true));

        Assert.Equal(1, pings);
        Assert.Equal(1, hellos);
    }

    [Fact]
    public void Reader_RejectsHugeLengthsWithoutOverflow()
    {
        var writer = new NetWriter();
        writer.Write(int.MaxValue);
        writer.Write(1);
        var reader = new NetReader(writer.ToArray());
        reader.ReadInt();
        reader.Set(writer.ToArray(), 0, writer.Length);
        Assert.Throws<InvalidOperationException>(() => reader.ReadBytes());
    }

    [Fact]
    public void ReadCount_ChecksRemainingBytes()
    {
        var writer = new NetWriter();
        writer.Write(3);
        writer.Write(1L);
        writer.Write(2L);
        writer.Write(3L);
        Assert.Equal(3, new NetReader(writer.ToArray()).ReadCount(8));

        var big = new NetWriter();
        big.Write(1_000_000);
        big.Write(1L);
        Assert.Throws<InvalidOperationException>(() => new NetReader(big.ToArray()).ReadCount(8));

        var negative = new NetWriter();
        negative.Write(-1);
        Assert.Throws<InvalidOperationException>(() => new NetReader(negative.ToArray()).ReadCount());
    }

    [Fact]
    public void QueryRateLimiter_LimitsBytesPerAddress()
    {
        var limiter = new QueryRateLimiter(1000, 100);
        var a = IPAddress.Parse("10.0.0.1");
        var b = IPAddress.Parse("10.0.0.2");

        Assert.True(limiter.TryConsume(a, 600, 0));
        Assert.False(limiter.TryConsume(a, 600, 0));
        Assert.True(limiter.TryConsume(b, 600, 0));
        Assert.False(limiter.TryConsume(a, 600, 1));
        Assert.True(limiter.TryConsume(a, 600, 3));
    }
}
