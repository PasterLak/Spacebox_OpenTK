using Engine.Audio;

namespace Engine.Tests.Audio;

public class PcmDownmixTests
{
    private static byte[] Pcm16(params short[] samples)
    {
        var data = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            data[i * 2] = (byte)samples[i];
            data[i * 2 + 1] = (byte)(samples[i] >> 8);
        }
        return data;
    }

    private static short Sample16(byte[] data, int index) => (short)(data[index * 2] | (data[index * 2 + 1] << 8));

    [Fact]
    public void StereoToMono16_AveragesChannels()
    {
        var mono = PcmDownmix.StereoToMono(Pcm16(1000, 3000, -2000, -4000, short.MaxValue, short.MaxValue), 16);

        Assert.Equal(6, mono.Length);
        Assert.Equal(2000, Sample16(mono, 0));
        Assert.Equal(-3000, Sample16(mono, 1));
        Assert.Equal(short.MaxValue, Sample16(mono, 2));
    }

    [Fact]
    public void StereoToMono8_AveragesUnsignedChannels()
    {
        var mono = PcmDownmix.StereoToMono(new byte[] { 100, 200, 0, 255 }, 8);

        Assert.Equal(new byte[] { 150, 128 }, mono);
    }

    [Fact]
    public void UnsupportedBitDepth_Throws()
    {
        Assert.Throws<NotSupportedException>(() => PcmDownmix.StereoToMono(new byte[6], 24));
    }
}
