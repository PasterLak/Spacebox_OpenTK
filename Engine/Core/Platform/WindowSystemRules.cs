namespace Engine.Platform;

public enum WindowSystem
{
    Auto,
    X11,
    Wayland,
    None
}

public static class WindowSystemRules
{
    public static bool TryParse(string text, out WindowSystem system)
    {
        system = WindowSystem.Auto;
        switch ((text ?? "").Trim().ToLowerInvariant())
        {
            case "":
            case "auto":
                return true;
            case "x11":
            case "xorg":
            case "xwayland":
                system = WindowSystem.X11;
                return true;
            case "wayland":
                system = WindowSystem.Wayland;
                return true;
            case "none":
            case "headless":
                system = WindowSystem.None;
                return true;
            default:
                return false;
        }
    }

    public static IReadOnlyList<WindowSystem> Candidates(bool isLinux, WindowSystem requested, bool headless, Func<string, string> environment)
    {
        if (headless && requested is WindowSystem.Auto or WindowSystem.None) return new[] { WindowSystem.None };
        if (requested == WindowSystem.None || !isLinux) return new[] { WindowSystem.Auto };
        if (requested == WindowSystem.X11) return new[] { WindowSystem.X11 };
        if (requested == WindowSystem.Wayland) return new[] { WindowSystem.Wayland };

        if (HasWayland(environment)) return new[] { WindowSystem.Wayland, WindowSystem.X11 };
        return new[] { WindowSystem.X11, WindowSystem.Wayland };
    }

    public static bool HasWayland(Func<string, string> environment)
    {
        return !string.IsNullOrEmpty(environment("WAYLAND_DISPLAY"));
    }

    public static bool SupportsWindowPosition(WindowSystem system) => system != WindowSystem.Wayland;

    public static bool SupportsWindowIcon(WindowSystem system) => system != WindowSystem.Wayland;
}
