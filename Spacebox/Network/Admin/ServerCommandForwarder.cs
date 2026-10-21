using Engine;

namespace Spacebox.Network.Admin;

public static class ServerCommandForwarder
{
    public static string BuildLine(string name, string[] args) => args.Length == 0 ? name : name + " " + string.Join(' ', args);

    public static bool TryForward(string name, string[] args)
    {
        if (!GameSession.IsRemoteClient) return false;

        ChatService.SendCommandToServer(BuildLine(name, args));
        Debug.Log($"Sent to the server: {BuildLine(name, args)}");
        return true;
    }
}
