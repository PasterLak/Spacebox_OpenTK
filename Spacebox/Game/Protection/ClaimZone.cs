using OpenTK.Mathematics;

namespace Spacebox.Game.Protection;

public static class ClaimZone
{
    public static bool Contains(Vector3i center, int radius, Vector3i cell)
    {
        return Math.Abs(cell.X - center.X) <= radius && Math.Abs(cell.Y - center.Y) <= radius && Math.Abs(cell.Z - center.Z) <= radius;
    }

    public static bool Overlap(Vector3i a, int radiusA, Vector3i b, int radiusB)
    {
        int reach = radiusA + radiusB;
        return Math.Abs(a.X - b.X) <= reach && Math.Abs(a.Y - b.Y) <= reach && Math.Abs(a.Z - b.Z) <= reach;
    }

    public static bool BoxIntersects(Vector3i center, int radius, Vector3i min, Vector3i max)
    {
        return max.X >= center.X - radius && min.X <= center.X + radius
            && max.Y >= center.Y - radius && min.Y <= center.Y + radius
            && max.Z >= center.Z - radius && min.Z <= center.Z + radius;
    }
}
