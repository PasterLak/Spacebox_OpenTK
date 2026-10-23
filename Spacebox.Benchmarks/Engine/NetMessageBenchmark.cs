using BenchmarkDotNet.Attributes;
using Engine.Networking;

namespace Spacebox.Benchmarks.Engine;

[MemoryDiagnoser]
public class NetMessageBenchmark
{
    public struct BenchmarkPositionMessage : INetMessage
    {
        public uint NetId;
        public float X, Y, Z;

        public void Serialize(NetWriter writer)
        {
            writer.Write(NetId);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
        }

        public void Deserialize(NetReader reader)
        {
            NetId = reader.ReadUInt();
            X = reader.ReadFloat();
            Y = reader.ReadFloat();
            Z = reader.ReadFloat();
        }
    }

    private readonly NetMessageRegistry _registry = new NetMessageRegistry();
    private readonly NetWriter _writer = new NetWriter(1024);
    private readonly NetReader _reader = new NetReader();
    private float _sum;

    [GlobalSetup]
    public void Setup()
    {
        _registry.Register<BenchmarkPositionMessage>((peer, message) => _sum += message.X);
    }

    [Benchmark]
    public int WriteAndDispatch()
    {
        _writer.Reset();
        _registry.Write(_writer, new BenchmarkPositionMessage { NetId = 7, X = 1f, Y = 2f, Z = 3f });
        _reader.Set(_writer.Buffer, 0, _writer.Length);
        _registry.Dispatch(1, _reader);
        return _writer.Length;
    }
}
