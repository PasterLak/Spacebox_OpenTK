using Engine.Platform;

namespace Engine.Tests.Platform;

public sealed class GpuSelectionRulesTests
{
    [Fact]
    public void WindowsPreference_IsWrittenOnlyWhenThePlayerChoseNothing()
    {
        Assert.Equal("GpuPreference=2;", GpuSelectionRules.WindowsPreferenceToWrite(null));
        Assert.Equal("GpuPreference=2;", GpuSelectionRules.WindowsPreferenceToWrite(""));
        Assert.Null(GpuSelectionRules.WindowsPreferenceToWrite("GpuPreference=1;"));
        Assert.Null(GpuSelectionRules.WindowsPreferenceToWrite("SwapEffectUpgradeEnable=1;GpuPreference=0;"));
        Assert.Equal("SwapEffectUpgradeEnable=1;GpuPreference=2;", GpuSelectionRules.WindowsPreferenceToWrite("SwapEffectUpgradeEnable=1"));
    }

    [Fact]
    public void Preference_IsNotStoredForTheSharedDotnetHost()
    {
        Assert.False(GpuSelectionRules.CanStorePreferenceFor(@"C:\Program Files\dotnet\dotnet.exe"));
        Assert.False(GpuSelectionRules.CanStorePreferenceFor(null));
        Assert.True(GpuSelectionRules.CanStorePreferenceFor(@"C:\Games\Spacebox\Spacebox.exe"));
    }

    [Fact]
    public void LinuxOffload_OnlyWithTwoGpusAndNoPlayerSettings()
    {
        Func<string, string> none = _ => null;
        Assert.Empty(GpuSelectionRules.LinuxOffloadVariables(1, true, none));

        var nvidia = GpuSelectionRules.LinuxOffloadVariables(2, true, none);
        Assert.Equal("1", nvidia["__NV_PRIME_RENDER_OFFLOAD"]);
        Assert.Equal("nvidia", nvidia["__GLX_VENDOR_LIBRARY_NAME"]);

        var mesa = GpuSelectionRules.LinuxOffloadVariables(2, false, none);
        Assert.Equal("1", mesa["DRI_PRIME"]);
        Assert.False(mesa.ContainsKey("__GLX_VENDOR_LIBRARY_NAME"));

        Assert.Empty(GpuSelectionRules.LinuxOffloadVariables(2, false, name => name == "DRI_PRIME" ? "0" : null));
    }

    [Fact]
    public void DrmCards_CountsGpusButNotConnectors()
    {
        Assert.Equal(2, GpuSelectionRules.CountDrmCards(new[] { "card0", "card0-eDP-1", "card1", "card1-HDMI-A-1", "renderD128", "version" }));
        Assert.Equal(0, GpuSelectionRules.CountDrmCards(new[] { "card", "renderD128" }));
    }
}
