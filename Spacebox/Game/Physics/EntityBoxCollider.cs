using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Physics;

public static class EntityBoxCollider
{
    public const float Skin = 0.001f;

    public static float Sweep(SpaceEntity entity, Vector3 min, Vector3 max, int axis, float distance)
    {
        if (distance == 0f) return 0f;

        int axisA = (axis + 1) % 3;
        int axisB = (axis + 2) % 3;
        int minA = (int)MathF.Floor(min[axisA] + Skin);
        int maxA = (int)MathF.Ceiling(max[axisA] - Skin) - 1;
        int minB = (int)MathF.Floor(min[axisB] + Skin);
        int maxB = (int)MathF.Ceiling(max[axisB] - Skin) - 1;

        if (distance > 0f)
        {
            int start = (int)MathF.Ceiling(max[axis] - 2f * Skin);
            int end = (int)MathF.Ceiling(max[axis] + distance) - 1;
            for (int c = start; c <= end; c++)
            {
                if (IsLayerSolid(entity, axis, c, axisA, minA, maxA, axisB, minB, maxB))
                    return MathF.Min(distance, MathF.Max(0f, c - max[axis] - Skin));
            }
            return distance;
        }

        int from = (int)MathF.Floor(min[axis] + 2f * Skin) - 1;
        int to = (int)MathF.Floor(min[axis] + distance);
        for (int c = from; c >= to; c--)
        {
            if (IsLayerSolid(entity, axis, c, axisA, minA, maxA, axisB, minB, maxB))
                return MathF.Max(distance, -MathF.Max(0f, min[axis] - (c + 1) - Skin));
        }
        return distance;
    }

    public static bool Overlaps(SpaceEntity entity, Vector3 min, Vector3 max)
    {
        int minX = (int)MathF.Floor(min.X + Skin), maxX = (int)MathF.Ceiling(max.X - Skin) - 1;
        int minY = (int)MathF.Floor(min.Y + Skin), maxY = (int)MathF.Ceiling(max.Y - Skin) - 1;
        int minZ = (int)MathF.Floor(min.Z + Skin), maxZ = (int)MathF.Ceiling(max.Z - Skin) - 1;

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            if (IsSolid(entity, new Vector3i(x, y, z))) return true;
        }
        return false;
    }

    private static bool IsLayerSolid(SpaceEntity entity, int axis, int layer, int axisA, int minA, int maxA, int axisB, int minB, int maxB)
    {
        var cell = new Vector3i();
        cell[axis] = layer;
        for (int a = minA; a <= maxA; a++)
        {
            cell[axisA] = a;
            for (int b = minB; b <= maxB; b++)
            {
                cell[axisB] = b;
                if (IsSolid(entity, cell)) return true;
            }
        }
        return false;
    }

    private static bool IsSolid(SpaceEntity entity, Vector3i cell)
    {
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return false;
        var block = chunk.Blocks[index.X, index.Y, index.Z];
        return block != null && !block.IsAir && !block.IsMeshHidden;
    }
}
