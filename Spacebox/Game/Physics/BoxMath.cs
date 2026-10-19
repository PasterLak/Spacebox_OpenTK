using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Physics;

public static class BoxMath
{
    public static bool OverlapsOnAxis(float boxMin, float boxMax, float moverMin, float moverMax, float skin)
    {
        return boxMax > moverMin + skin && boxMin < moverMax - skin;
    }

    public static float ClipDistance(float moverMin, float moverMax, float boxMin, float boxMax, float allowed, bool positive, float skin)
    {
        if (positive)
        {
            if (boxMin < moverMax - 2f * skin) return allowed;
            return MathF.Min(allowed, MathF.Max(0f, boxMin - moverMax - skin));
        }

        if (boxMax > moverMin + 2f * skin) return allowed;
        return MathF.Max(allowed, -MathF.Max(0f, moverMin - boxMax - skin));
    }

    public static bool Contains(Vector3 min, Vector3 max, Vector3 point)
    {
        return point.X >= min.X && point.Y >= min.Y && point.Z >= min.Z &&
               point.X <= max.X && point.Y <= max.Y && point.Z <= max.Z;
    }

    public static bool RayIntersects(Vector3 origin, Vector3 direction, Vector3 min, Vector3 max, out float distance, out Vector3SByte normal)
    {
        distance = 0f;
        normal = Vector3SByte.Zero;

        if (Contains(min, max, origin))
        {
            normal = NearestFaceNormal(origin, min, max);
            return true;
        }

        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;
        int entryAxis = -1;

        for (int axis = 0; axis < 3; axis++)
        {
            float o = origin[axis];
            float d = direction[axis];

            if (MathF.Abs(d) < 1e-8f)
            {
                if (o < min[axis] || o > max[axis]) return false;
                continue;
            }

            float t1 = (min[axis] - o) / d;
            float t2 = (max[axis] - o) / d;
            if (t1 > t2) (t1, t2) = (t2, t1);

            if (t1 > tMin)
            {
                tMin = t1;
                entryAxis = axis;
            }

            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax) return false;
        }

        if (entryAxis < 0 || tMax < 0f || tMin < 0f) return false;

        distance = tMin;
        int sign = direction[entryAxis] > 0f ? -1 : 1;
        normal = entryAxis switch
        {
            0 => new Vector3SByte(sign, 0, 0),
            1 => new Vector3SByte(0, sign, 0),
            _ => new Vector3SByte(0, 0, sign)
        };
        return true;
    }

    public static Vector3SByte NearestFaceNormal(Vector3 point, Vector3 min, Vector3 max)
    {
        float best = point.X - min.X;
        var normal = new Vector3SByte(-1, 0, 0);

        Check(max.X - point.X, new Vector3SByte(1, 0, 0));
        Check(point.Y - min.Y, new Vector3SByte(0, -1, 0));
        Check(max.Y - point.Y, new Vector3SByte(0, 1, 0));
        Check(point.Z - min.Z, new Vector3SByte(0, 0, -1));
        Check(max.Z - point.Z, new Vector3SByte(0, 0, 1));
        return normal;

        void Check(float distance, Vector3SByte candidate)
        {
            if (distance >= best) return;
            best = distance;
            normal = candidate;
        }
    }
}
