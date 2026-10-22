using Engine.Audio;

namespace Engine.Tests.Audio;

public sealed class AudioDeviceSwitchRulesTests
{
    [Fact]
    public void Reopens_WhenTheDefaultChangesOrTheDeviceIsGone()
    {
        Assert.True(AudioDeviceSwitchRules.ShouldReopen(true, true));
        Assert.True(AudioDeviceSwitchRules.ShouldReopen(false, false));
        Assert.False(AudioDeviceSwitchRules.ShouldReopen(false, true));
    }
}
