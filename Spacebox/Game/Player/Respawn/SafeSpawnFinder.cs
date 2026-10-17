using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player.Respawn;

public static class SafeSpawnFinder
{
    private const int MaxSearchRadius = 16;
    private const float EyeHeight = 1.5f;

    public static Vector3 FindFreePosition(Vector3 eyeWorldPosition)
    {
        var sector = World.CurrentSector;
        if (sector == null || !sector.TryGetNearestEntity(eyeWorldPosition, out var entity)) return eyeWorldPosition;

        var local = entity.WorldToEntityLocal(eyeWorldPosition);
        var feet = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y - EyeHeight), (int)MathF.Floor(local.Z));
        if (IsFree(entity, feet)) return eyeWorldPosition;

        return TryFindAround(entity, feet, 1, out var position) ? position : eyeWorldPosition;
    }

    public static bool TryFindNear(SpaceEntity entity, Vector3i cell, out Vector3 eyeWorldPosition)
    {
        return TryFindAround(entity, cell, 1, out eyeWorldPosition);
    }

    private static bool TryFindAround(SpaceEntity entity, Vector3i center, int startRadius, out Vector3 eyeWorldPosition)
    {
        for (int radius = startRadius; radius <= MaxSearchRadius; radius++)
        {
            for (int i = 0; i <= radius * 2; i++)
            {
                int dy = (i & 1) == 0 ? -(i >> 1) : (i + 1) >> 1;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Max(Math.Abs(dy), Math.Abs(dz))) != radius) continue;

                        var feet = new Vector3i(center.X + dx, center.Y + dy, center.Z + dz);
                        if (!IsFree(entity, feet)) continue;

                        eyeWorldPosition = entity.EntityLocalToWorld(new Vector3(feet.X + 0.5f, feet.Y + EyeHeight, feet.Z + 0.5f));
                        return true;
                    }
                }
            }
        }

        eyeWorldPosition = default;
        return false;
    }

    private static bool IsFree(SpaceEntity entity, Vector3i feet)
    {
        return !IsSolid(entity, feet) && !IsSolid(entity, new Vector3i(feet.X, feet.Y + 1, feet.Z));
    }

    private static bool IsSolid(SpaceEntity entity, Vector3i cell)
    {
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return false;
        var block = chunk.Blocks[index.X, index.Y, index.Z];
        return block != null && !block.IsAir;
    }
}
