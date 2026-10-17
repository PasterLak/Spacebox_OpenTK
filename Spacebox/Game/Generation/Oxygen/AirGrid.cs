using System.Runtime.CompilerServices;
using Engine;

namespace Spacebox.Game.Generation.Oxygen;

public static class AirGrid
{
    private const int ChunkOffset = SpaceEntity.SizeChunksHalf;
    private const int ChunksPerAxis = SpaceEntity.SizeChunks;

    public const int ChunkSlots = ChunksPerAxis * ChunksPerAxis * ChunksPerAxis;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInsideEntity(int x, int y, int z)
    {
        const int half = SpaceEntity.SizeBlocksHalf;
        return x >= -half && x < half && y >= -half && y < half && z >= -half && z < half;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ChunkKey(int x, int y, int z)
    {
        return ChunkSlot((x >> 5) + ChunkOffset, (y >> 5) + ChunkOffset, (z >> 5) + ChunkOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ChunkKey(Vector3SByte chunkIndex)
    {
        return ChunkSlot(chunkIndex.X + ChunkOffset, chunkIndex.Y + ChunkOffset, chunkIndex.Z + ChunkOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CellIndex(int x, int y, int z)
    {
        return ((x & 31) << 10) | ((y & 31) << 5) | (z & 31);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ChunkSlot(int cx, int cy, int cz)
    {
        return (cx * ChunksPerAxis + cy) * ChunksPerAxis + cz;
    }
}
