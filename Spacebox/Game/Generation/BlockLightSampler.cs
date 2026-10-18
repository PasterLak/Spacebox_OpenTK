using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;

public static class BlockLightSampler
{
    public static Vector3 Sample(Vector3 worldPosition, SpaceEntity? hint = null)
    {
        var entity = hint != null && Contains(hint, worldPosition) ? hint : FindEntity(worldPosition);
        if (entity == null) return Vector3.Zero;

        var local = entity.WorldToEntityLocal(worldPosition) - new Vector3(0.5f);
        var origin = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
        var t = local - new Vector3(origin.X, origin.Y, origin.Z);

        var sum = Vector3.Zero;
        float weightSum = 0f;

        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            if (!TryGetCellLight(entity, origin + new Vector3i(x, y, z), out var light)) continue;

            float weight = (x == 1 ? t.X : 1f - t.X) * (y == 1 ? t.Y : 1f - t.Y) * (z == 1 ? t.Z : 1f - t.Z);
            sum += light * weight;
            weightSum += weight;
        }

        return weightSum > 1e-4f ? sum / weightSum : Vector3.Zero;
    }

    private static SpaceEntity? FindEntity(Vector3 worldPosition)
    {
        var sector = World.SectorAt(worldPosition);
        if (sector == null || !sector.TryGetNearestEntity(worldPosition, out var entity) || entity == null) return null;
        return Contains(entity, worldPosition) ? entity : null;
    }

    private static bool Contains(SpaceEntity entity, Vector3 worldPosition)
    {
        return entity.IsPositionWithinEntitySize(entity.ToEntitySpace(worldPosition));
    }

    private static bool TryGetCellLight(SpaceEntity entity, Vector3i cell, out Vector3 light)
    {
        light = Vector3.Zero;
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return true;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null) return true;
        if (!block.IsAir && !block.IsTransparent) return false;

        light = block.LightColor.ToVector3();
        return true;
    }
}
