using Engine.Networking;
using OpenTK.Mathematics;

namespace Engine.Tests.Networking;

public class NetSerializationTests
{
    private static NetReader ReaderFor(NetWriter writer)
    {
        return new NetReader(writer.ToArray());
    }

    [Fact]
    public void Primitives_RoundTrip()
    {
        var writer = new NetWriter(4);
        writer.Write((byte)200);
        writer.Write((sbyte)-100);
        writer.Write(true);
        writer.Write(false);
        writer.Write((short)-12345);
        writer.Write((ushort)54321);
        writer.Write(int.MinValue);
        writer.Write(uint.MaxValue);
        writer.Write(long.MinValue);
        writer.Write(ulong.MaxValue);
        writer.Write(-1.5f);
        writer.Write(Math.PI);

        var reader = ReaderFor(writer);

        Assert.Equal(200, reader.ReadByte());
        Assert.Equal(-100, reader.ReadSByte());
        Assert.True(reader.ReadBool());
        Assert.False(reader.ReadBool());
        Assert.Equal(-12345, reader.ReadShort());
        Assert.Equal(54321, reader.ReadUShort());
        Assert.Equal(int.MinValue, reader.ReadInt());
        Assert.Equal(uint.MaxValue, reader.ReadUInt());
        Assert.Equal(long.MinValue, reader.ReadLong());
        Assert.Equal(ulong.MaxValue, reader.ReadULong());
        Assert.Equal(-1.5f, reader.ReadFloat());
        Assert.Equal(Math.PI, reader.ReadDouble());
        Assert.True(reader.EndOfData);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("Привет, космос 🚀")]
    public void Strings_RoundTrip(string value)
    {
        var writer = new NetWriter();
        writer.Write(value);

        Assert.Equal(value, ReaderFor(writer).ReadString());
    }

    [Fact]
    public void NullString_RoundTripsAsNull()
    {
        var writer = new NetWriter();
        writer.Write((string)null!);

        Assert.Null(ReaderFor(writer).ReadString());
    }

    [Fact]
    public void Vectors_RoundTrip()
    {
        var writer = new NetWriter();
        writer.Write(new Vector2(1f, -2f));
        writer.Write(new Vector3(3f, 4f, -5f));
        writer.Write(new Vector3i(-6, 7, 8));
        writer.Write(new Quaternion(0.1f, 0.2f, 0.3f, 0.9f));

        var reader = ReaderFor(writer);

        Assert.Equal(new Vector2(1f, -2f), reader.ReadVector2());
        Assert.Equal(new Vector3(3f, 4f, -5f), reader.ReadVector3());
        Assert.Equal(new Vector3i(-6, 7, 8), reader.ReadVector3i());
        Assert.Equal(new Quaternion(0.1f, 0.2f, 0.3f, 0.9f), reader.ReadQuaternion());
    }

    [Fact]
    public void Bytes_RoundTripWithLength_RawWithout()
    {
        var writer = new NetWriter();
        writer.WriteBytes(new byte[] { 1, 2, 3 });
        writer.WriteRaw(new byte[] { 9, 8 });

        var reader = ReaderFor(writer);

        Assert.Equal(new byte[] { 1, 2, 3 }, reader.ReadBytes());
        Assert.Equal(2, reader.Remaining);
        Assert.Equal(new byte[] { 9, 8 }, reader.ReadRemaining().ToArray());
        Assert.True(reader.EndOfData);
    }

    [Fact]
    public void Writer_GrowsBeyondInitialCapacity()
    {
        var writer = new NetWriter(1);
        for (int i = 0; i < 1000; i++) writer.Write(i);

        var reader = ReaderFor(writer);
        for (int i = 0; i < 1000; i++) Assert.Equal(i, reader.ReadInt());
    }

    [Fact]
    public void Reset_ClearsLength()
    {
        var writer = new NetWriter();
        writer.Write(42);
        writer.Reset();
        writer.Write((byte)7);

        Assert.Equal(1, writer.Length);
        Assert.Equal(7, ReaderFor(writer).ReadByte());
    }

    [Fact]
    public void ReadingPastEnd_Throws()
    {
        var writer = new NetWriter();
        writer.Write((short)1);

        var reader = ReaderFor(writer);
        reader.ReadShort();

        Assert.ThrowsAny<Exception>(() => reader.ReadInt());
    }

    [Fact]
    public void Set_ReadsOnlyGivenRange()
    {
        var reader = new NetReader();
        reader.Set(new byte[] { 0, 5, 6, 0 }, 1, 2);

        Assert.Equal(5, reader.ReadByte());
        Assert.Equal(6, reader.ReadByte());
        Assert.True(reader.EndOfData);
    }
}
