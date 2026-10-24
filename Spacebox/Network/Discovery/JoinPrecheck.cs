namespace Spacebox.Network.Discovery;

public static class JoinPrecheck
{
    public static string Check(ServerStatus status, string clientVersion)
    {
        if (status == null || string.IsNullOrEmpty(status.GameVersion) || status.GameVersion == clientVersion) return null;
        return $"The server runs version {status.GameVersion}, you have {clientVersion}. Both need the same game version.";
    }
}
