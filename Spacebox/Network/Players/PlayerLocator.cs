using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Network.Players;

public static class PlayerLocator
{
    public static bool TryFind(string nameOrPeer, out ConnectedPlayer player)
    {
        player = null;
        if (string.IsNullOrWhiteSpace(nameOrPeer)) return false;
        if (int.TryParse(nameOrPeer, out int peer) && PlayerRegistry.TryGet(peer, out player)) return true;
        return PlayerRegistry.TryGetByName(nameOrPeer, out player);
    }

    public static bool TryGetPose(int peer, out Vector3 position, out Vector3 front)
    {
        position = Vector3.Zero;
        front = -Vector3.UnitZ;

        var network = AstronautNetwork.Find(peer);
        if (network == null) return false;

        if (network.IsOwner && network.Owner is Astronaut astronaut)
        {
            position = astronaut.Position;
            front = astronaut.Front;
            return true;
        }

        position = network.TargetPosition;
        front = Vector3.Transform(-Vector3.UnitZ, network.TargetRotation);
        return true;
    }
}
