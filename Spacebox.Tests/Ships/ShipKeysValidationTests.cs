using Engine.Networking;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ShipKeysValidationTests
{
    [Theory]
    [InlineData(ShipKeys.None)]
    [InlineData(ShipKeys.MouseLeft)]
    [InlineData(ShipKeys.MouseRight)]
    [InlineData((int)Keys.Space)]
    [InlineData((int)Keys.D1)]
    [InlineData((int)Keys.Menu)]
    public void IsValid_AcceptsKnownKeys(int key)
    {
        Assert.True(ShipKeys.IsValid(key));
    }

    [Theory]
    [InlineData((int)Keys.Unknown)]
    [InlineData(-5)]
    [InlineData(1)]
    [InlineData(100000)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void IsValid_RejectsUnknownValues(int key)
    {
        Assert.False(ShipKeys.IsValid(key));
    }

    [Fact]
    public void SetKey_IgnoresInvalidKey()
    {
        var bindings = new ConsoleBindings(2, false);
        int before = bindings.GetKey(0);

        bindings.SetKey(0, 100000);

        Assert.Equal(before, bindings.GetKey(0));
    }

    [Fact]
    public void CodecRead_ReplacesInvalidKeysWithNone()
    {
        var writer = new NetWriter(64);
        writer.Write((byte)3);
        writer.Write(false);
        writer.Write(100000);
        writer.Write((int)Keys.G);
        writer.Write(-77);
        writer.Write(false);
        writer.Write((ushort)0);

        var bindings = ConsoleBindingsCodec.FromBytes(writer.ToArray());

        Assert.Equal(ShipKeys.None, bindings.GetKey(0));
        Assert.Equal((int)Keys.G, bindings.GetKey(1));
        Assert.Equal(ShipKeys.None, bindings.GetKey(2));
    }
}
