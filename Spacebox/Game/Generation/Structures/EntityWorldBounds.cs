using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;

public static class EntityWorldBounds
{
    public static void Compute(Vector3 entityMin, Vector3 entityMax, Vector3 pivot, Quaternion orientation, out Vector3 min, out Vector3 max)
    {
        if (orientation == Quaternion.Identity)
        {
            min = entityMin;
            max = entityMax;
            return;
        }

        min = new Vector3(float.MaxValue);
        max = new Vector3(float.MinValue);

        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3(
                (i & 1) == 0 ? entityMin.X : entityMax.X,
                (i & 2) == 0 ? entityMin.Y : entityMax.Y,
                (i & 4) == 0 ? entityMin.Z : entityMax.Z);

            var world = pivot + Vector3.Transform(corner - pivot, orientation);
            min = Vector3.ComponentMin(min, world);
            max = Vector3.ComponentMax(max, world);
        }
    }

    public static bool Overlaps(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
    {
        return minA.X <= maxB.X && maxA.X >= minB.X &&
               minA.Y <= maxB.Y && maxA.Y >= minB.Y &&
               minA.Z <= maxB.Z && maxA.Z >= minB.Z;
    }
}
