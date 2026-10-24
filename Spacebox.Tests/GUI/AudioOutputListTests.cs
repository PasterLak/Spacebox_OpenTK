using Engine.Audio;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Tests.GUI;

public class AudioOutputListTests
{
    [Fact]
    public void List_StartsWithTheSystemDefault()
    {
        var list = AudioOutputList.Build(new[] { "Speakers", "Headphones" }, "");

        Assert.Equal(new[] { AudioOutputList.SystemDefault, "Speakers", "Headphones" }, list.Labels);
        Assert.Equal(0, list.IndexOf(""));
        Assert.Equal("", list.DeviceAt(0));
        Assert.Equal("Headphones", list.DeviceAt(2));
    }

    [Fact]
    public void Labels_ShowOnlyTheDeviceName()
    {
        var list = AudioOutputList.Build(new[] { "OpenAL Soft on Speakers (Realtek(R) Audio)", "Headphones" }, "OpenAL Soft on USB Headset");

        Assert.Equal("Speakers (Realtek(R) Audio)", list.Labels[1]);
        Assert.Equal("Headphones", list.Labels[2]);
        Assert.Equal("USB Headset" + AudioOutputList.MissingSuffix, list.Labels[3]);
        Assert.Equal("OpenAL Soft on Speakers (Realtek(R) Audio)", list.DeviceAt(1));
        Assert.Equal("OpenAL Soft", AudioDeviceNames.Display("OpenAL Soft"));
    }

    [Fact]
    public void UnpluggedChoice_StaysInTheList()
    {
        var list = AudioOutputList.Build(new[] { "Speakers" }, "Headphones");

        Assert.Equal("Headphones" + AudioOutputList.MissingSuffix, list.Labels[2]);
        Assert.Equal(2, list.IndexOf("Headphones"));
        Assert.Equal("Headphones", list.DeviceAt(2));
    }

    [Fact]
    public void Choice_FallsBackToTheDefaultWhenMissing()
    {
        Assert.Equal("Headphones", AudioOutputChoice.Resolve("Headphones", new[] { "Speakers", "Headphones" }));
        Assert.Null(AudioOutputChoice.Resolve("Headphones", new[] { "Speakers" }));
        Assert.Null(AudioOutputChoice.Resolve("", new[] { "Speakers" }));
    }

    [Fact]
    public void Reopen_WhenTheChosenDeviceChangesOrDisappears()
    {
        Assert.True(AudioOutputChoice.NeedsReopen("Headphones", null, false, true));
        Assert.False(AudioOutputChoice.NeedsReopen("Headphones", "Headphones", true, true));
        Assert.True(AudioOutputChoice.NeedsReopen(null, "Headphones", false, true));
        Assert.True(AudioOutputChoice.NeedsReopen(null, null, true, true));
        Assert.False(AudioOutputChoice.NeedsReopen(null, null, false, true));
    }
}
