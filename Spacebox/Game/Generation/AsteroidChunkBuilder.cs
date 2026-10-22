using Engine;
using Engine.Utils;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Structures;
using Spacebox.Generation;

namespace Spacebox.Game.Generation;

public static class AsteroidChunkBuilder
{
    public const int PaddedSize = Chunk.Size + 2;

    public static Block[,,] BuildPaddedBlocks(AsteroidVoxelDataGenerator voxelGen, AsteroidData asteroid, int seed, long entityId, Vector3SByte idx)
    {
        return BuildPaddedBlocks(voxelGen, asteroid, seed, entityId, idx, AsteroidGenerationContext.Legacy);
    }

    public static Block[,,] BuildPaddedBlocks(AsteroidVoxelDataGenerator voxelGen, AsteroidData asteroid, int seed, long entityId, Vector3SByte idx, AsteroidGenerationContext context)
    {
        var paddedData = voxelGen.GeneratePaddedDataForChunk(idx);
        var chunkSeed = SeedHelper.GetChunkIdInt(entityId, idx);

        if (asteroid.UsePerlinWorms)
        {
            var worms = context.WormsFor(asteroid);
            var wormParams = new WormParameters(
                worms.MinCount,
                worms.MaxCount,
                worms.DiameterInBlocks,
                worms.PathDeviation,
                worms.MaxTunnelLength,
                ComputeStep(worms.DiameterInBlocks, worms.StepSize),
                seed);

            PerlinWorms.CarvePaddedChunk(paddedData, idx, seed, wormParams);
        }

        var cavities = context.CavitiesFor(asteroid);
        if (cavities != null)
            AsteroidCavities.Carve(paddedData, idx, AsteroidCavities.Create(seed, context.Radius, cavities));

        var oreGen = new AsteroidOreGenerator(asteroid, chunkSeed, context, idx);
        oreGen.ApplyOres(ref paddedData);

        var paddedBlocks = PaddedArrayPools.Blocks.Rent();
        for (int x = 0; x < PaddedSize; x++)
        {
            for (int y = 0; y < PaddedSize; y++)
            {
                for (int z = 0; z < PaddedSize; z++)
                {
                    paddedBlocks[x, y, z] = GameAssets.CreateBlockFromId((short)paddedData[x, y, z]);
                }
            }
        }

        PaddedArrayPools.Voxels.Return(paddedData);
        return paddedBlocks;
    }

    public static void GetChunkRange(int diameterBlocks, out int minChunk, out int maxChunk)
    {
        float halfSize = diameterBlocks * 0.5f;
        minChunk = (int)MathF.Floor(-halfSize / Chunk.Size);
        maxChunk = (int)MathF.Floor(halfSize / Chunk.Size);
    }

    private static float ComputeStep(byte wormDiameter, float overlapK = 0.75f)
    {
        float step = (wormDiameter * 0.5f) * overlapK;
        return step < 1f ? 1f : step;
    }
}
