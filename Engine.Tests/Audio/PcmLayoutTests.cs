using Engine.Audio;

namespace Engine.Tests.Audio;

public class PcmLayoutTests
{
    [Theory]
    [InlineData(1001, 1, 16, 1000)]
    [InlineData(1003, 2, 16, 1000)]
    [InlineData(1000, 2, 16, 1000)]
    [InlineData(7, 1, 8, 7)]
    [InlineData(3, 2, 16, 0)]
    public void AlignedLength_CutsPartialFrames(int length, int channels, int bits, int expected)
    {
        Assert.Equal(expected, PcmLayout.AlignedLength(length, channels, bits));
    }
}
