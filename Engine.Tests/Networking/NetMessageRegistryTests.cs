using Engine.Networking;

namespace Engine.Tests.Networking;

public class NetMessageRegistryTests
{
    private struct PositionMessage : INetMessage
    {
        public uint NetId;
        public float X;

        public void Serialize(NetWriter writer)
        {
            writer.Write(NetId);
            writer.Write(X);
        }

        public void Deserialize(NetReader reader)
        {
            NetId = reader.ReadUInt();
            X = reader.ReadFloat();
        }
    }

    [Fact]
    public void GetId_IsStableHashOfFullName()
    {
        Assert.Equal(StableHash.Compute(typeof(PositionMessage).FullName), NetMessageRegistry.GetId<PositionMessage>());
    }

    [Fact]
    public void Dispatch_DeliversStructMessage()
    {
        var registry = new NetMessageRegistry();
        PositionMessage received = default;
        int sender = 0;
        registry.Register<PositionMessage>((peer, message) => { sender = peer; received = message; });
        var writer = new NetWriter();
        registry.Write(writer, new PositionMessage { NetId = 42, X = 1.5f });
        var reader = new NetReader();
        reader.Set(writer.Buffer, 0, writer.Length);

        Assert.True(registry.Dispatch(3, reader));
        Assert.Equal(3, sender);
        Assert.Equal(42u, received.NetId);
        Assert.Equal(1.5f, received.X);
    }

    [Fact]
    public void DispatchLocal_InvokesTypedHandler()
    {
        var registry = new NetMessageRegistry();
        float x = 0f;
        registry.Register<PositionMessage>((peer, message) => x = message.X);

        registry.DispatchLocal(1, new PositionMessage { X = 4f });

        Assert.Equal(4f, x);
    }

    [Fact]
    public void WriteAndDispatch_DoNotAllocate()
    {
        var registry = new NetMessageRegistry();
        float sum = 0f;
        registry.Register<PositionMessage>((peer, message) => sum += message.X);
        var writer = new NetWriter();
        var reader = new NetReader();

        void Roundtrip()
        {
            writer.Reset();
            registry.Write(writer, new PositionMessage { NetId = 1, X = 1f });
            reader.Set(writer.Buffer, 0, writer.Length);
            registry.Dispatch(1, reader);
        }

        for (int i = 0; i < 3; i++) Roundtrip();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) Roundtrip();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(103f, sum);
    }
}
