using Engine.Networking;

namespace Spacebox.Network;

public static class LaunchOptions
{
    public static bool IsDedicated { get; private set; }
    public static bool IsHidden { get; private set; }
    public static string ConnectAddress { get; private set; }
    public static int ConnectPort { get; private set; } = NetworkManager.DefaultPort;
    public static string PlayerName { get; private set; }
    public static string EditorGameSet { get; private set; }
    public static string EditorPanel { get; private set; }
    public static bool UiShowcase { get; private set; }
    public static Engine.Platform.WindowSystem WindowSystem { get; private set; }

    public static bool HasConnectTarget => !string.IsNullOrEmpty(ConnectAddress);

    public static void Parse(string[] args)
    {
        ParseWindowSystem(Environment.GetEnvironmentVariable("SPACEBOX_WINDOW_SYSTEM"));
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLowerInvariant();
            string next = i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "--server":
                case "--dedicated":
                    IsDedicated = true;
                    break;
                case "--ui-showcase":
                    UiShowcase = true;
                    break;
                case "--hidden":
                    IsHidden = true;
                    break;
                case "--connect" when next != null:
                    ParseAddress(next);
                    i++;
                    break;
                case "--window-system" when next != null:
                    ParseWindowSystem(next);
                    i++;
                    break;
                case "--name" when next != null:
                    PlayerName = next;
                    i++;
                    break;
                case "--editor" when next != null:
                    EditorGameSet = next;
                    i++;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        EditorPanel = args[i + 1];
                        i++;
                    }
                    break;
            }
        }
    }

    private static void ParseWindowSystem(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (Engine.Platform.WindowSystemRules.TryParse(value, out var system)) WindowSystem = system;
        else Engine.Debug.Warning($"[Window] Unknown window system '{value}', use x11, wayland or auto");
    }

    private static void ParseAddress(string value)
    {
        Discovery.ServerAddressValidator.Resolve(value, ConnectPort, out var host, out var port);
        ConnectAddress = host;
        ConnectPort = port;
    }
}
