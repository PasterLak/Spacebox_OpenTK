

using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GameMath;

public class WorldMath
{
    public bool IsPositionInside(Vector3 worldPosition)
    {

        float totalSize = World.SizeSectors * Sector.SizeBlocks;

        float halfSize = totalSize / 2.0f;

        bool inX = worldPosition.X >= -halfSize && worldPosition.X <= halfSize;
        bool inY = worldPosition.Y >= -halfSize && worldPosition.Y <= halfSize;
        bool inZ = worldPosition.Z >= -halfSize && worldPosition.Z <= halfSize;

        return inX && inY && inZ;
    }

    public float DistanceToBox(Vector3 point, BoundingBox box)
    {
        float dx = Math.Max(box.Min.X - point.X, 0f);
        dx = Math.Max(dx, point.X - box.Max.X);
        float dy = Math.Max(box.Min.Y - point.Y, 0f);
        dy = Math.Max(dy, point.Y - box.Max.Y);
        float dz = Math.Max(box.Min.Z - point.Z, 0f);
        dz = Math.Max(dz, point.Z - box.Max.Z);
        return dx * dx + dy * dy + dz * dz;
    }

    public float DistanceToEdge(Vector3 local, int sectorSize, Vector3i dir)
    {
        if (dir.X < 0) return local.X;
        if (dir.X > 0) return sectorSize - local.X;
        if (dir.Y < 0) return local.Y;
        if (dir.Y > 0) return sectorSize - local.Y;
        if (dir.Z < 0) return local.Z;
        if (dir.Z > 0) return sectorSize - local.Z;
        return float.MaxValue;
    }

    public Vector3 GetRandomPointAroundPosition(Vector3 center, float minDistance, float maxDistance)
    {
        if (minDistance > maxDistance)
        {
            var temp = minDistance;
            minDistance = maxDistance;
            maxDistance = temp;
        }

        Random random = new Random();

        float distance = minDistance + (float)random.NextDouble() * (maxDistance - minDistance);

        float theta = (float)random.NextDouble() * MathF.PI * 2f;
        float phi = MathF.Acos(1f - 2f * (float)random.NextDouble());

        float x = distance * MathF.Sin(phi) * MathF.Cos(theta);
        float y = distance * MathF.Sin(phi) * MathF.Sin(theta);
        float z = distance * MathF.Cos(phi);

        return center + new Vector3(x, y, z);
    }

}
