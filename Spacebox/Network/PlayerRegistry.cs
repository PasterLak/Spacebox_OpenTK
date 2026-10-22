using Engine.Networking;

namespace Spacebox.Network;

public sealed class ConnectedPlayer
{
    public int Peer { get; init; }
    public string Name { get; init; }
    public string SkinColor { get; init; }
    public NetworkObject PlayerObject { get; set; }
    public string LastPlayerJson { get; set; }
    public DateTime JoinedAtUtc { get; init; } = DateTime.UtcNow;
}

public static class PlayerRegistry
{
    private static readonly Dictionary<int, ConnectedPlayer> _players = new Dictionary<int, ConnectedPlayer>();

    public static IReadOnlyCollection<ConnectedPlayer> All => _players.Values;

    public static void Add(ConnectedPlayer player)
    {
        _players[player.Peer] = player;
    }

    public static bool Remove(int peer, out ConnectedPlayer player)
    {
        return _players.Remove(peer, out player);
    }

    public static bool TryGet(int peer, out ConnectedPlayer player)
    {
        return _players.TryGetValue(peer, out player);
    }

    public static bool TryGetByName(string name, out ConnectedPlayer player)
    {
        foreach (var candidate in _players.Values)
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                player = candidate;
                return true;
            }
        }

        player = null;
        return false;
    }

    public static string GetName(int peer)
    {
        return _players.TryGetValue(peer, out var player) ? player.Name : $"Peer{peer}";
    }

    public static void Clear()
    {
        _players.Clear();
    }
}
