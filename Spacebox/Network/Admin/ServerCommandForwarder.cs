using System.Globalization;
using Engine;

namespace Spacebox.Network.Admin;

public static class ServerCommandForwarder
{
    public static bool ShouldForwardTeleport(string[] args) => args.Length > 0 && !float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    public static bool ShouldForwardTeleportToPlayer(string[] args) => args.Length >= 2;

    public static string BuildLine(string name, string[] args) => args.Length == 0 ? name : name + " " + string.Join(' ', args);

    public static bool TryForward(string name, string[] args)
    {
        if (!GameSession.IsRemoteClient) return false;

        ChatService.SendCommandToServer(BuildLine(name, args));
        Debug.Log($"Sent to the server: {BuildLine(name, args)}");
        return true;
    }
}
