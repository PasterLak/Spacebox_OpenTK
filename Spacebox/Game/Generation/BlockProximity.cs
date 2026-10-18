using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation;

public static class BlockProximity
{
    public static bool HasBlockWithin(Vector3i cell, int range, Func<Vector3SByte, Block[,,]?> getChunkBlocks)
    {
        if (range < 0) return false;

        int rangeSquared = range * range;
        var min = cell - new Vector3i(range);
        var max = cell + new Vector3i(range);

        for (int cx = FloorDiv(min.X); cx <= FloorDiv(max.X); cx++)
        for (int cy = FloorDiv(min.Y); cy <= FloorDiv(max.Y); cy++)
        for (int cz = FloorDiv(min.Z); cz <= FloorDiv(max.Z); cz++)
        {
            if (!IsChunkIndex(cx) || !IsChunkIndex(cy) || !IsChunkIndex(cz)) continue;

            var origin = new Vector3i(cx * Chunk.Size, cy * Chunk.Size, cz * Chunk.Size);
            if (DistanceSquaredToChunk(cell, origin) > rangeSquared) continue;

            var blocks = getChunkBlocks(new Vector3SByte((sbyte)cx, (sbyte)cy, (sbyte)cz));
            if (blocks != null && ChunkHasBlockWithin(blocks, origin, cell, min, max, rangeSquared)) return true;
        }

        return false;
    }

    private static bool ChunkHasBlockWithin(Block[,,] blocks, Vector3i origin, Vector3i cell, Vector3i min, Vector3i max, int rangeSquared)
    {
        int x0 = Math.Max(min.X, origin.X), x1 = Math.Min(max.X, origin.X + Chunk.Size - 1);
        int y0 = Math.Max(min.Y, origin.Y), y1 = Math.Min(max.Y, origin.Y + Chunk.Size - 1);
        int z0 = Math.Max(min.Z, origin.Z), z1 = Math.Min(max.Z, origin.Z + Chunk.Size - 1);

        for (int x = x0; x <= x1; x++)
        {
            int dx = x - cell.X;
            int dx2 = dx * dx;
            if (dx2 > rangeSquared) continue;

            for (int y = y0; y <= y1; y++)
            {
                int dy = y - cell.Y;
                int dxy2 = dx2 + dy * dy;
                if (dxy2 > rangeSquared) continue;

                for (int z = z0; z <= z1; z++)
                {
                    int dz = z - cell.Z;
                    if (dxy2 + dz * dz > rangeSquared) continue;

                    var block = blocks[x - origin.X, y - origin.Y, z - origin.Z];
                    if (block != null && !block.IsAir) return true;
                }
            }
        }

        return false;
    }

    private static int DistanceSquaredToChunk(Vector3i cell, Vector3i origin)
    {
        int dx = Gap(cell.X, origin.X);
        int dy = Gap(cell.Y, origin.Y);
        int dz = Gap(cell.Z, origin.Z);
        return dx * dx + dy * dy + dz * dz;
    }

    private static int Gap(int value, int chunkMin)
    {
        int chunkMax = chunkMin + Chunk.Size - 1;
        if (value < chunkMin) return chunkMin - value;
        if (value > chunkMax) return value - chunkMax;
        return 0;
    }

    private static bool IsChunkIndex(int value)
    {
        return value >= sbyte.MinValue && value <= sbyte.MaxValue;
    }

    private static int FloorDiv(int value)
    {
        return (int)MathF.Floor(value / (float)Chunk.Size);
    }
}
