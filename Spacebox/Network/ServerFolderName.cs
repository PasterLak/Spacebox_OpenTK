using Spacebox.Game.Generation;

namespace Spacebox.Network;

public static class ServerFolderName
{
    public static string For(string host, int port)
    {
        string normalized = string.IsNullOrWhiteSpace(host) ? "unknown" : host.Trim().ToLowerInvariant();
        return World.MakeSafeFolderName($"{normalized}_{port}");
    }

    public static string Current => For(GameSession.HostAddress, GameSession.Port);
}
