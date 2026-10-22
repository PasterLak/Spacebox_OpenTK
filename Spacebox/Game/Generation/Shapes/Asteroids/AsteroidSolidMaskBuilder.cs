using Engine;
using Spacebox.Game.Generation.Structures;
using Spacebox.Generation;

namespace Spacebox.Game.Generation.Shapes.Asteroids;

public static class AsteroidSolidMaskBuilder
{
    public const int MaxSize = 256;

    public static AsteroidSolidMask TryBuild(AsteroidVoxelDataGenerator voxelGen, AsteroidData asteroid, int seed, AsteroidGenerationContext context, int diameterBlocks)
    {
        if (!context.RemovesFloatingBlocks) return null;

        AsteroidChunkBuilder.GetChunkRange(diameterBlocks, out int minChunk, out int maxChunk);
        int origin = minChunk * Chunk.Size;
        int size = (maxChunk - minChunk + 1) * Chunk.Size;
        if (size > MaxSize) return null;

        var mask = new AsteroidSolidMask(origin, size);
        for (int cx = minChunk; cx <= maxChunk; cx++)
        for (int cy = minChunk; cy <= maxChunk; cy++)
        for (int cz = minChunk; cz <= maxChunk; cz++)
        {
            var index = new Vector3SByte(cx, cy, cz);
            var padded = voxelGen.GeneratePaddedDataForChunk(index);
            AsteroidChunkBuilder.CarveTunnels(padded, asteroid, seed, index, context);

            for (int x = 1; x <= Chunk.Size; x++)
            for (int y = 1; y <= Chunk.Size; y++)
            for (int z = 1; z <= Chunk.Size; z++)
            {
                if (padded[x, y, z] != 0)
                    mask.Set(cx * Chunk.Size + x - 1, cy * Chunk.Size + y - 1, cz * Chunk.Size + z - 1, true);
            }

            PaddedArrayPools.Voxels.Return(padded);
        }

        mask.KeepLargestComponent();
        return mask;
    }
}
