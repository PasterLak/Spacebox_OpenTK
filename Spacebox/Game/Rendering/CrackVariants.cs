using OpenTK.Mathematics;

namespace Spacebox.Game.Rendering;

public static class CrackVariants
{
    public const int Count = 8;
    public const int Orientations = 8;

    public static uint Hash(Vector3i cell)
    {
        unchecked
        {
            uint h = (uint)cell.X * 73856093u ^ (uint)cell.Y * 19349663u ^ (uint)cell.Z * 83492791u;
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            return h;
        }
    }

    public static int Variant(Vector3i cell) => (int)(Hash(cell) & (Count - 1));

    public static int Orientation(Vector3i cell, int face) => (int)(((Hash(cell) >> 3) ^ (uint)face) & (Orientations - 1));
}
