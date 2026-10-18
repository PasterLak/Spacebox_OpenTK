using Spacebox.Game.Generation.Tools;

namespace Spacebox.Tests;

public class PackingToolsTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(255, 1)]
    [InlineData(18, 254)]
    public void Bytes_RoundTrip(byte high, byte low)
    {
        PackingTools.UnpackBytes(PackingTools.PackBytes(high, low), out var h, out var l);

        Assert.Equal(high, h);
        Assert.Equal(low, l);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 1)]
    [InlineData(short.MinValue, short.MaxValue)]
    public void TwoShorts_RoundTrip(short a, short b)
    {
        PackingTools.UnpackShorts(PackingTools.PackShorts(a, b), out short ra, out short rb);

        Assert.Equal(a, ra);
        Assert.Equal(b, rb);
    }

    [Theory]
    [InlineData(1, 64, 3, 2)]
    [InlineData(-1, -2, -3, -4)]
    [InlineData(short.MaxValue, short.MinValue, 0, 255)]
    public void FourShorts_RoundTrip(short a, short b, short c, short d)
    {
        PackingTools.UnpackShorts(PackingTools.PackShorts(a, b, c, d), out var ra, out var rb, out var rc, out var rd);

        Assert.Equal((a, b, c, d), (ra, rb, rc, rd));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, int.MaxValue)]
    [InlineData(int.MinValue, -1)]
    public void Ints_RoundTrip(int high, int low)
    {
        PackingTools.UnpackInts(PackingTools.PackInts(high, low), out var h, out var l);

        Assert.Equal(high, h);
        Assert.Equal(low, l);
    }

    [Fact]
    public void SignedConversions_AreLossless()
    {
        Assert.Equal((sbyte)-3, PackingTools.ByteToSByte(PackingTools.SByteToByte(-3)));
        Assert.Equal(-7L, PackingTools.ULongToLong(PackingTools.LongToULong(-7L)));
    }
}
