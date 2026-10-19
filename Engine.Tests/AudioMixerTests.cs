using Engine.Audio;

namespace Engine.Tests;

public sealed class AudioMixerTests : IDisposable
{
    public AudioMixerTests()
    {
        AudioMixer.Reset();
    }

    public void Dispose()
    {
        AudioMixer.Reset();
    }

    [Fact]
    public void Gain_IsMasterTimesChannel()
    {
        AudioMixer.Master = 0.5f;
        AudioMixer.SetVolume(AudioChannel.Music, 0.4f);

        Assert.Equal(0.2f, AudioMixer.GetGain(AudioChannel.Music), 5);
        Assert.Equal(0.5f, AudioMixer.GetGain(AudioChannel.Effects), 5);
    }

    [Fact]
    public void Muted_SilencesEveryChannel()
    {
        AudioMixer.Muted = true;

        Assert.Equal(0f, AudioMixer.GetGain(AudioChannel.UI));
        AudioMixer.Muted = false;
        Assert.Equal(1f, AudioMixer.GetGain(AudioChannel.UI));
    }

    [Fact]
    public void Volumes_AreClamped()
    {
        AudioMixer.Master = 3f;
        AudioMixer.SetVolume(AudioChannel.Ambient, -1f);

        Assert.Equal(1f, AudioMixer.Master);
        Assert.Equal(0f, AudioMixer.GetVolume(AudioChannel.Ambient));
    }
}
