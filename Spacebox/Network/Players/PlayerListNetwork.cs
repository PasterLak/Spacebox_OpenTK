using Engine;
using Engine.Networking;
using Spacebox.Network.Moderation;

namespace Spacebox.Network;

public struct PlayerListEntry
{
    public int Peer;
    public string Name;
    public int Ping;
    public bool IsMuted;
    public bool IsHost;
    public bool IsAdmin;
}

public struct PlayerListMessage : INetMessage
{
    public List<PlayerListEntry> Players;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Players.Count);
        for (int i = 0; i < Players.Count; i++)
        {
            var player = Players[i];
            writer.Write(player.Peer);
            writer.Write(player.Name);
            writer.Write(player.Ping);
            writer.Write(player.IsMuted);
            writer.Write(player.IsHost);
            writer.Write(player.IsAdmin);
        }
    }

    public void Deserialize(NetReader reader)
    {
        int count = reader.ReadInt();
        Players = new List<PlayerListEntry>(count);
        for (int i = 0; i < count; i++)
        {
            Players.Add(new PlayerListEntry
            {
                Peer = reader.ReadInt(),
                Name = reader.ReadString(),
                Ping = reader.ReadInt(),
                IsMuted = reader.ReadBool(),
                IsHost = reader.ReadBool(),
                IsAdmin = reader.ReadBool()
            });
        }
    }
}

public static class PlayerListNetwork
{
    private const float RefreshInterval = 1f;

    private static readonly List<PlayerListEntry> _players = new List<PlayerListEntry>();
    private static float _timer;
    private static bool _dirty;

    public static IReadOnlyList<PlayerListEntry> Players => _players;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<PlayerListMessage>(OnPlayerList);
    }

    public static void MarkDirty()
    {
        _dirty = true;
    }

    public static void Update()
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;

        _timer += Time.Delta;
        if (!_dirty && _timer < RefreshInterval) return;

        _timer = 0f;
        _dirty = false;
        Rebuild(network);

        if (network.Peers.Count > 0)
            network.SendToClients(new PlayerListMessage { Players = new List<PlayerListEntry>(_players) });
    }

    public static void Clear()
    {
        _players.Clear();
        _timer = 0f;
        _dirty = false;
    }

    private static void Rebuild(NetworkManager network)
    {
        _players.Clear();

        foreach (var player in PlayerRegistry.All)
        {
            _players.Add(new PlayerListEntry
            {
                Peer = player.Peer,
                Name = player.Name,
                Ping = network.GetRoundTripMilliseconds(player.Peer),
                IsMuted = ServerModeration.IsMuted(player.Name),
                IsHost = player.Peer == network.LocalPeerId,
                IsAdmin = Admin.ServerAdmins.ShowsBadge(player.Name)
            });
        }

        _players.Sort((a, b) => a.IsHost != b.IsHost ? (a.IsHost ? -1 : 1) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static void OnPlayerList(int sender, PlayerListMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        _players.Clear();
        _players.AddRange(message.Players);
    }
}
