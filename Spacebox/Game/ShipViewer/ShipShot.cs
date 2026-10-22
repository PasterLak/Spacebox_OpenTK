using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipShotResult
{
    public List<Vector3> Path { get; } = new List<Vector3>();
    public List<Vector3i> ChangedCells { get; } = new List<Vector3i>();
    public bool Hit { get; set; }
    public Vector3 End => Path[^1];

    public float Length
    {
        get
        {
            float length = 0f;
            for (int i = 1; i < Path.Count; i++) length += (Path[i] - Path[i - 1]).Length;
            return length;
        }
    }
}

public static class ShipShot
{
    private const float SurfaceOffset = Engine.Physics.Ray.RicochetSurfaceOffset;

    public static ShipShotResult Fire(ShipBlockGrid grid, Vector3 origin, Vector3 direction, float maxDistance, in ProjectileParameters projectile)
    {
        var result = new ShipShotResult();
        result.Path.Add(origin);
        direction = Vector3.Normalize(direction);

        int damage = projectile.DamageBlocks;
        int destroyed = 0;
        int ricochets = 0;
        float remaining = maxDistance;

        while (grid.Raycast(origin, direction, remaining, out var cell, out var normal, out float distance))
        {
            var point = origin + direction * distance;
            var block = grid.Get(cell);
            result.Hit = true;
            remaining -= distance;

            if (CanRicochet(projectile, ricochets, direction, normal))
            {
                ricochets++;
                result.Path.Add(point);

                var n = new Vector3(normal.X, normal.Y, normal.Z);
                direction = Vector3.Normalize(direction - 2f * Vector3.Dot(direction, n) * n);
                origin = point + n * SurfaceOffset;
                continue;
            }

            byte durability = block.Durability;
            if (projectile.Penetration < durability || !grid.Damage(cell, damage, result.ChangedCells))
            {
                result.Path.Add(point);
                return result;
            }

            damage = Math.Max(0, damage - durability);
            destroyed++;
            if (destroyed >= projectile.MaximumBlocksCanDestroy || damage <= 0)
            {
                result.Path.Add(point);
                return result;
            }

            origin = point + direction * SurfaceOffset;
        }

        result.Path.Add(origin + direction * MathF.Max(0f, remaining));
        return result;
    }

    private static bool CanRicochet(in ProjectileParameters projectile, int ricochets, Vector3 direction, Vector3i normal)
    {
        if (projectile.RicochetAngle <= 0 || ricochets >= projectile.PossibleRicochets || normal == Vector3i.Zero) return false;

        var n = new Vector3(normal.X, normal.Y, normal.Z);
        float dot = Math.Clamp(Vector3.Dot(-direction, n), -1f, 1f);
        float angle = 90f - MathHelper.RadiansToDegrees(MathF.Acos(dot));
        return angle <= projectile.RicochetAngle;
    }
}
