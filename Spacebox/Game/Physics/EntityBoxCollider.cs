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
        float allowed = distance;

        if (distance > 0f)
        {
            int start = (int)MathF.Floor(max[axis] - 2f * Skin);
            int end = (int)MathF.Ceiling(max[axis] + distance) - 1;
            for (int c = start; c <= end && c <= max[axis] + allowed; c++)
                allowed = ClipLayer(entity, min, max, axis, c, axisA, minA, maxA, axisB, minB, maxB, allowed, true);
            return allowed;
        }

        int from = (int)MathF.Floor(min[axis] + 2f * Skin);
        int to = (int)MathF.Floor(min[axis] + distance);
        for (int c = from; c >= to && c + 1 >= min[axis] + allowed; c--)
            allowed = ClipLayer(entity, min, max, axis, c, axisA, minA, maxA, axisB, minB, maxB, allowed, false);
        return allowed;
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
            if (!BlockCollision.TryGetSolidBounds(entity, new Vector3i(x, y, z), out var boxMin, out var boxMax)) continue;
            if (BoxMath.OverlapsOnAxis(boxMin.X, boxMax.X, min.X, max.X, Skin) &&
                BoxMath.OverlapsOnAxis(boxMin.Y, boxMax.Y, min.Y, max.Y, Skin) &&
                BoxMath.OverlapsOnAxis(boxMin.Z, boxMax.Z, min.Z, max.Z, Skin))
                return true;
        }
        return false;
    }

    private static float ClipLayer(SpaceEntity entity, Vector3 min, Vector3 max, int axis, int layer, int axisA, int minA, int maxA, int axisB, int minB, int maxB, float allowed, bool positive)
    {
        var cell = new Vector3i();
        cell[axis] = layer;
        for (int a = minA; a <= maxA; a++)
        {
            cell[axisA] = a;
            for (int b = minB; b <= maxB; b++)
            {
                cell[axisB] = b;
                if (!BlockCollision.TryGetSolidBounds(entity, cell, out var boxMin, out var boxMax)) continue;
                if (!BoxMath.OverlapsOnAxis(boxMin[axisA], boxMax[axisA], min[axisA], max[axisA], Skin)) continue;
                if (!BoxMath.OverlapsOnAxis(boxMin[axisB], boxMax[axisB], min[axisB], max[axisB], Skin)) continue;
                allowed = BoxMath.ClipDistance(min[axis], max[axis], boxMin[axis], boxMax[axis], allowed, positive, Skin);
            }
        }
        return allowed;
    }
}
