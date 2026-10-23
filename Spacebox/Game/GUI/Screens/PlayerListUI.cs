using Engine.Networking;
using Spacebox.Game.GUI.Canvas.Windows;
using Spacebox.Network;
using Spacebox.Network.Moderation;

namespace Spacebox.Game.GUI;

public static class PlayerListUI
{
    public const int GoodPing = 80;
    public const int MediumPing = 200;

    public static IReadOnlyList<PlayerListEntry> Players => PlayerListNetwork.Players;

    public static bool CanModerate => NetworkManager.Instance.IsServer && GameSession.HasLocalPlayer;

    public static int LocalPeer => NetworkManager.Instance.LocalPeerId;

    public static string Label(PlayerListEntry player, int localPeer)
    {
        string label = Spacebox.Network.Admin.PlayerBadges.Label(player.Name, player.IsAdmin);
        if (player.IsHost) label += " (host)";
        if (player.Peer == localPeer) label += " (you)";
        if (player.IsMuted) label += " [muted]";
        return label;
    }

    public static string PingText(PlayerListEntry player) => player.IsHost ? "-" : $"{player.Ping} ms";

    public static StatusKind PingKind(PlayerListEntry player)
    {
        if (player.IsHost) return StatusKind.Muted;
        if (player.Ping < GoodPing) return StatusKind.Good;
        return player.Ping < MediumPing ? StatusKind.Warning : StatusKind.Bad;
    }

    public static bool CanModeratePlayer(PlayerListEntry player, int localPeer) => !player.IsHost && player.Peer != localPeer;

    public static void Kick(PlayerListEntry player) => ServerModeration.Kick(player.Peer);

    public static void ToggleMute(PlayerListEntry player) => ServerModeration.SetMuted(player.Name, !player.IsMuted);

    public static void Ban(int peer) => ServerModeration.Ban(peer);
}
