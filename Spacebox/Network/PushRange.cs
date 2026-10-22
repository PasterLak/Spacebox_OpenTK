using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network;

public static class PushRange
{
    public const float MaxDistance = 6f;

    public static bool IsNear(int peer, Vector3 target)
    {
        if (peer == NetworkManager.Instance.LocalPeerId) return true;
        var player = Combat.PlayerHitboxes.FindPlayer(peer);
        return player != null && Vector3.DistanceSquared(player.Position, target) <= MaxDistance * MaxDistance;
    }
}
