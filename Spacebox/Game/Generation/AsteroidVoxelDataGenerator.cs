using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;
using Spacebox.Generation;
using System;

namespace Spacebox.Game.Generation
{
    public class AsteroidVoxelDataGenerator
    {
        private float blockSize;
        private Vector3 asteroidDimensions;
        private int threshold;
        private byte noiseOctaves;
        private float noiseScale;
        private int seed;
        private NoiseGenerator noiseGenerator;
        private AsteroidData asteroidData;
        private float[] layerThresholds;
        private float hollowRadius;

        public AsteroidVoxelDataGenerator(Vector3 asteroidDimensions, float blockSize, AsteroidData asteroidData, int seed)
            : this(asteroidDimensions, blockSize, asteroidData, seed, AsteroidGenerationContext.Legacy)
        {
        }

        public AsteroidVoxelDataGenerator(Vector3 asteroidDimensions, float blockSize, AsteroidData asteroidData, int seed, AsteroidGenerationContext context)
        {
            if (context.UsesRules) hollowRadius = asteroidDimensions.X * 0.5f * asteroidData.HollowCorePercent / 100f;
            this.asteroidData = asteroidData;
            this.asteroidDimensions = asteroidDimensions;
            this.blockSize = blockSize;
            this.threshold = asteroidData.DensityThreshold;
            this.noiseOctaves = asteroidData.NoiseOctaves;
            this.noiseScale = asteroidData.NoiseScale;
            this.seed = seed;

            CalculateLayerThresholds();
        }

        private void CalculateLayerThresholds()
        {
            layerThresholds = AsteroidLayers.Thresholds(asteroidData.Layers);
        }

        private int GetBlockIdFromDistance(float normalizedDistance)
        {
            int index = AsteroidLayers.IndexAt(layerThresholds, normalizedDistance);
            return index < 0 ? 1 : asteroidData.Layers[index].FillBlockID;
        }

        public int[,,] GeneratePaddedDataForChunk(Vector3SByte chunkIdx)
        {
            int paddedSize = Chunk.Size + 2;

            if (noiseGenerator == null)
            {
                noiseGenerator = new NoiseGenerator(seed);
                noiseGenerator.Masks.sphericalFalloffDistance = asteroidDimensions.X * 0.5f;
                noiseGenerator.Masks.sphericalGradient = true;
            }

            Vector3 regionSize = asteroidDimensions;
            float radius = asteroidDimensions.X * 0.5f;

            var chunkData = PaddedArrayPools.Voxels.Rent();

            for (int x = 0; x < paddedSize; x++)
            {
                for (int y = 0; y < paddedSize; y++)
                {
                    for (int z = 0; z < paddedSize; z++)
                    {
                        int gx = chunkIdx.X * Chunk.Size + (x - 1);
                        int gy = chunkIdx.Y * Chunk.Size + (y - 1);
                        int gz = chunkIdx.Z * Chunk.Size + (z - 1);

                        Vector3 pos = new Vector3(gx, gy, gz) * blockSize;
                        Vector3 samplePos = (pos + regionSize * 0.5f) * noiseScale;

                        byte noiseValue = noiseGenerator.PerlinNoise3DWithMask(
                            samplePos,
                            ref regionSize,
                            noiseOctaves,
                            noiseGenerator.Masks.SphericalMaskFunction
                        );

                        int blockId = 0;
                        if (noiseValue > threshold && (hollowRadius <= 0f || pos.Length >= hollowRadius))
                        {
                            float distance = pos.Length;
                            float normalized = 1.0f - (distance / radius);
                            normalized = MathF.Max(0f, MathF.Min(1f, normalized));
                            blockId = GetBlockIdFromDistance(normalized);
                        }

                        chunkData[x, y, z] = blockId;
                    }
                }
            }

            return chunkData;
        }
    }
}