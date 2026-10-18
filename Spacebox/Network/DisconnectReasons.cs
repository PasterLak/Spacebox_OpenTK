namespace Spacebox.Network;

public static class DisconnectReasons
{
    private static readonly Dictionary<string, string> _descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "ConnectionFailed", "Could not reach the server" },
        { "Timeout", "Connection timed out" },
        { "HostUnreachable", "Server is unreachable" },
        { "NetworkUnreachable", "Network is unreachable" },
        { "RemoteConnectionClose", "The server closed the connection" },
        { "DisconnectPeerCalled", "Disconnected" },
        { "ConnectionRejected", "Connection rejected by the server (wrong game version or server is full)" },
        { "InvalidProtocol", "Invalid protocol (different game version)" },
        { "UnknownHost", "Unknown host" },
        { "Reconnect", "Reconnected from another place" },
        { "PeerToPeerConnection", "Peer to peer connection" },
        { "PeerNotFound", "Server was not found" }
    };

    public static string Describe(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return "Disconnected";
        return _descriptions.TryGetValue(reason.Trim(), out var description) ? description : reason;
    }
}
