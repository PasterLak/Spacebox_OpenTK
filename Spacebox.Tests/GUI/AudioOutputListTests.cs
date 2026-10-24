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
