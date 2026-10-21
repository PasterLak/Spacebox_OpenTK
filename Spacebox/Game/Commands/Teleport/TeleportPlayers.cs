using Engine.Networking;
using Spacebox.Game.Player;
using Spacebox.Network;
using Spacebox.Network.Admin;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands.Teleport;

public static class TeleportPlayers
{
    public static void Collect(LocalAstronaut localPlayer, List<ITeleportable> results)
    {
        if (GameSession.HasLocalPlayer && localPlayer != null) results.Add(new LocalTeleportable(localPlayer));
        if (!NetworkManager.Instance.IsServer) return;

        foreach (var player in PlayerRegistry.All)
        {
            if (IsLocalName(player.Name)) continue;
            var network = AstronautNetwork.Find(player.Peer);
            if (network != null) results.Add(new RemoteTeleportable(player, network));
        }
    }

    public static ITeleportable FindSender(IReadOnlyList<ITeleportable> players)
    {
        string sender = ChatCommands.CurrentSender;
        if (sender != null) return TeleportSelector.FindByName(players, sender);

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] is LocalTeleportable) return players[i];
        }

        return null;
    }

    private static bool IsLocalName(string name)
    {
        return GameSession.HasLocalPlayer && string.Equals(name, GameSession.PlayerName, StringComparison.OrdinalIgnoreCase);
    }
}
