using Engine;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation;

public static class PaddedArrayPools
{
    public const int PaddedSize = Chunk.Size + 2;

    private static readonly int MaxRetained = Math.Max(8, Environment.ProcessorCount * 2);

    public static readonly ConcurrentPool<Block[,,]> Blocks = new ConcurrentPool<Block[,,]>(
        () => new Block[PaddedSize, PaddedSize, PaddedSize],
        array => Array.Clear(array),
        MaxRetained);

    public static readonly ConcurrentPool<int[,,]> Voxels = new ConcurrentPool<int[,,]>(
        () => new int[PaddedSize, PaddedSize, PaddedSize],
        null,
        MaxRetained);
}
