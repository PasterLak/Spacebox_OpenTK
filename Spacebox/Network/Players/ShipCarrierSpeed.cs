using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Network.Players;

public static class ShipCarrierSpeed
{
    public const int CarryDistance = 8;

    public static float Around(Vector3 previous, Vector3 next) => MathF.Max(At(previous), At(next));

    private static float At(Vector3 position)
    {
        var sector = World.CurrentSector;
        if (sector == null || !sector.TryGetNearestEntity(position, out var entity)) return 0f;

        var ship = entity?.Ship;
        if (ship == null || !ship.Motion.IsMoving || !entity.HasBlockNear(position, CarryDistance)) return 0f;

        return ship.GetPointVelocity(position).Length;
    }
}
