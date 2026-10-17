using Engine.Networking;

namespace Spacebox.Network;

public static class LaunchOptions
{
    public static bool IsDedicated { get; private set; }
    public static bool IsHidden { get; private set; }
    public static string ConnectAddress { get; private set; }
    public static int ConnectPort { get; private set; } = NetworkManager.DefaultPort;
    public static string PlayerName { get; private set; }

    public static bool HasConnectTarget => !string.IsNullOrEmpty(ConnectAddress);

    public static void Parse(string[] args)
    {
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
                case "--hidden":
                    IsHidden = true;
                    break;
                case "--connect" when next != null:
                    ParseAddress(next);
                    i++;
                    break;
                case "--name" when next != null:
                    PlayerName = next;
                    i++;
                    break;
            }
        }
    }

    private static void ParseAddress(string value)
    {
        int colon = value.LastIndexOf(':');
        if (colon > 0 && int.TryParse(value.Substring(colon + 1), out int port))
        {
            ConnectAddress = value.Substring(0, colon);
            ConnectPort = port;
            return;
        }

        ConnectAddress = value;
    }
}
