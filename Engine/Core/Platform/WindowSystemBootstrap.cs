using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GlfwPlatform = OpenTK.Windowing.GraphicsLibraryFramework.Platform;

namespace Engine.Platform;

public static class WindowSystemBootstrap
{
    private static readonly GLFWCallbacks.ErrorCallback ErrorCallback = OnGlfwError;

    public static WindowSystem Current { get; private set; } = WindowSystem.Auto;

    public static WindowSystem Initialize(WindowSystem requested, bool headless)
    {
        GLFWProvider.HonorOpenTK4UseWayland = false;
        GLFWProvider.SetErrorCallback(ErrorCallback);

        var candidates = WindowSystemRules.Candidates(OperatingSystem.IsLinux(), requested, headless, Environment.GetEnvironmentVariable);
        foreach (var candidate in candidates)
        {
            if (!TryInitialize(candidate)) continue;

            Current = candidate;
            if (OperatingSystem.IsLinux() || candidate == WindowSystem.None) Debug.Log($"[Window] Window system: {Describe(candidate)}");
            return candidate;
        }

        Debug.Error($"[Window] No window system could be started (tried {string.Join(", ", candidates)}). On Linux start the game inside a desktop session (X11 or Wayland); choose one with --window-system x11|wayland");
        return WindowSystem.Auto;
    }

    private static bool TryInitialize(WindowSystem system)
    {
        var platform = ToPlatform(system);
        if (platform != GlfwPlatform.Any)
        {
            if (!GLFW.PlatformSupported(platform)) return false;
            GLFW.InitHint(InitHintPlatform.Platform, platform);
        }

        if (GLFW.Init()) return true;
        GLFW.Terminate();
        return false;
    }

    private static GlfwPlatform ToPlatform(WindowSystem system) => system switch
    {
        WindowSystem.X11 => GlfwPlatform.X11,
        WindowSystem.Wayland => GlfwPlatform.Wayland,
        WindowSystem.None => GlfwPlatform.Null,
        _ => GlfwPlatform.Any
    };

    private static string Describe(WindowSystem system) => system switch
    {
        WindowSystem.X11 => "X11",
        WindowSystem.Wayland => "Wayland",
        WindowSystem.None => "none (no display, headless)",
        _ => "default"
    };

    private static void OnGlfwError(ErrorCode code, string description)
    {
        Debug.Warning($"[Window] GLFW {code}: {description}");
    }
}
