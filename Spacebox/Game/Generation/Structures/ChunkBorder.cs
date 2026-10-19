using Engine;

namespace Spacebox.Game.Generation;

public static class ChunkBorder
{
    public const int MaxTouchedNeighbors = 7;

    public static int GetTouchedNeighbors(Vector3Byte cell, byte size, Span<Vector3SByte> result)
    {
        int sideX = Side(cell.X, size);
        int sideY = Side(cell.Y, size);
        int sideZ = Side(cell.Z, size);
        int count = 0;

        for (int x = Math.Min(0, sideX); x <= Math.Max(0, sideX); x++)
        for (int y = Math.Min(0, sideY); y <= Math.Max(0, sideY); y++)
        for (int z = Math.Min(0, sideZ); z <= Math.Max(0, sideZ); z++)
        {
            if (x == 0 && y == 0 && z == 0) continue;
            result[count++] = new Vector3SByte(x, y, z);
        }

        return count;
    }

    private static int Side(byte value, byte size)
    {
        if (value == 0) return -1;
        if (value == size - 1) return 1;
        return 0;
    }
}
