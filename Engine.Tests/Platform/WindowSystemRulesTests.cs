using Engine.Platform;

namespace Engine.Tests.Platform;

public sealed class WindowSystemRulesTests
{
    private static Func<string, string> Env(string display = null, string wayland = null) => name => name switch
    {
        "DISPLAY" => display,
        "WAYLAND_DISPLAY" => wayland,
        _ => null
    };

    [Theory]
    [InlineData("x11", WindowSystem.X11)]
    [InlineData("XWayland", WindowSystem.X11)]
    [InlineData("wayland", WindowSystem.Wayland)]
    [InlineData("auto", WindowSystem.Auto)]
    [InlineData("", WindowSystem.Auto)]
    [InlineData("headless", WindowSystem.None)]
    public void Names_AreParsed(string text, WindowSystem expected)
    {
        Assert.True(WindowSystemRules.TryParse(text, out var system));
        Assert.Equal(expected, system);
    }

    [Fact]
    public void UnknownName_IsRejected()
    {
        Assert.False(WindowSystemRules.TryParse("mir", out _));
    }

    [Fact]
    public void WaylandSession_PrefersWaylandWithX11Fallback()
    {
        Assert.Equal(new[] { WindowSystem.Wayland, WindowSystem.X11 }, WindowSystemRules.Candidates(true, WindowSystem.Auto, false, Env(":0", "wayland-0")));
        Assert.Equal(new[] { WindowSystem.Wayland, WindowSystem.X11 }, WindowSystemRules.Candidates(true, WindowSystem.Auto, false, Env(wayland: "wayland-0")));
    }

    [Fact]
    public void X11Session_UsesX11WithWaylandFallback()
    {
        Assert.Equal(new[] { WindowSystem.X11, WindowSystem.Wayland }, WindowSystemRules.Candidates(true, WindowSystem.Auto, false, Env(":0")));
        Assert.Equal(new[] { WindowSystem.X11, WindowSystem.Wayland }, WindowSystemRules.Candidates(true, WindowSystem.Auto, false, Env()));
    }

    [Fact]
    public void ExplicitChoice_IsTheOnlyCandidate()
    {
        Assert.Equal(new[] { WindowSystem.X11 }, WindowSystemRules.Candidates(true, WindowSystem.X11, false, Env(":0", "wayland-0")));
        Assert.Equal(new[] { WindowSystem.Wayland }, WindowSystemRules.Candidates(true, WindowSystem.Wayland, false, Env(":0")));
    }

    [Fact]
    public void DedicatedServers_RunWithoutAWindowUnlessOneIsForced()
    {
        Assert.Equal(new[] { WindowSystem.None }, WindowSystemRules.Candidates(true, WindowSystem.Auto, true, Env()));
        Assert.Equal(new[] { WindowSystem.None }, WindowSystemRules.Candidates(false, WindowSystem.Auto, true, Env()));
        Assert.Equal(new[] { WindowSystem.X11 }, WindowSystemRules.Candidates(true, WindowSystem.X11, true, Env(":0")));
    }

    [Fact]
    public void OtherSystems_KeepTheDefaultAndGamesNeverRunWithoutAWindow()
    {
        Assert.Equal(new[] { WindowSystem.Auto }, WindowSystemRules.Candidates(false, WindowSystem.Wayland, false, Env()));
        Assert.Equal(new[] { WindowSystem.Auto }, WindowSystemRules.Candidates(true, WindowSystem.None, false, Env(":0")));
    }

    [Fact]
    public void Wayland_HasNoWindowPositionOrIcon()
    {
        Assert.False(WindowSystemRules.SupportsWindowPosition(WindowSystem.Wayland));
        Assert.False(WindowSystemRules.SupportsWindowIcon(WindowSystem.Wayland));
        Assert.True(WindowSystemRules.SupportsWindowPosition(WindowSystem.X11));
        Assert.True(WindowSystemRules.SupportsWindowIcon(WindowSystem.Auto));
    }
}
