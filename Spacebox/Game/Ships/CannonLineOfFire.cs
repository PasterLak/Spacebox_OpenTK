using Engine.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Combat;

namespace Spacebox.Game.Ships;

public static class CannonLineOfFire
{
    public const float CheckRange = 512f;
    private const int NoIgnoredPeer = int.MinValue;

    public static bool IsClear(SpaceEntity entity, CannonBlock cannon)
    {
        var origin = entity.EntityLocalToWorld(cannon.MuzzleLocal);
        var direction = entity.DirectionToWorldSpace(cannon.AimDirection).Normalized();

        return !HitsOwnEntity(entity, cannon, origin, direction) && !HitsShooter(cannon, origin, direction);
    }

    private static bool HitsOwnEntity(SpaceEntity entity, CannonBlock cannon, OpenTK.Mathematics.Vector3 origin, OpenTK.Mathematics.Vector3 direction)
    {
        if (!entity.Raycast(new Ray(origin, direction, CheckRange), out var hit)) return false;
        if (hit.chunk == null) return true;
        return !ReferenceEquals(entity.GetStructureOrigin(hit.chunk, hit.blockPositionIndex), cannon);
    }

    private static bool HitsShooter(CannonBlock cannon, OpenTK.Mathematics.Vector3 origin, OpenTK.Mathematics.Vector3 direction)
    {
        if (cannon.ShooterPeer < 0) return false;
        return PlayerHitboxes.Raycast(origin, direction, CheckRange, NoIgnoredPeer, 0f, out var hit) && hit.Peer == cannon.ShooterPeer;
    }
}
