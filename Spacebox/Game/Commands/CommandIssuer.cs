using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Network;
using Spacebox.Network.Admin;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands;

public static class CommandIssuer
{
    public static bool TryGetPosition(LocalAstronaut localPlayer, out Vector3 position)
    {
        position = Vector3.Zero;
        if (GameSession.HasLocalPlayer && localPlayer != null)
        {
            position = localPlayer.Position;
            return true;
        }

        string sender = ChatCommands.CurrentSender;
        return sender != null && PlayerLocator.TryFind(sender, out var player) && PlayerLocator.TryGetPose(player.Peer, out position, out _);
    }
}
