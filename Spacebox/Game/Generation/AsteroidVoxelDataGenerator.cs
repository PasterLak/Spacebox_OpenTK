using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Shapes.Asteroids;
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
        private readonly AsteroidShape shape;

        public AsteroidVoxelDataGenerator(Vector3 asteroidDimensions, float blockSize, AsteroidData asteroidData, int seed)
            : this(asteroidDimensions, blockSize, asteroidData, seed, AsteroidGenerationContext.Legacy)
        {
        }

        public AsteroidVoxelDataGenerator(Vector3 asteroidDimensions, float blockSize, AsteroidData asteroidData, int seed, AsteroidGenerationContext context)
        {
            if (context.UsesRules) hollowRadius = asteroidDimensions.X * 0.5f * asteroidData.HollowCorePercent / 100f;
            shape = context.Shape;
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
            if (shape != null) return GenerateShapedChunk(chunkIdx, null);

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

        public int[,,] GeneratePaddedDataForChunk(Vector3SByte chunkIdx, AsteroidSolidMask mask)
        {
            return shape != null ? GenerateShapedChunk(chunkIdx, mask) : GeneratePaddedDataForChunk(chunkIdx);
        }

        private int[,,] GenerateShapedChunk(Vector3SByte chunkIdx, AsteroidSolidMask mask)
        {
            int paddedSize = Chunk.Size + 2;
            EnsureNoise();

            Vector3 regionSize = asteroidDimensions;
            float radius = asteroidDimensions.X * 0.5f;
            float halfRoughness = shape.Roughness * 0.5f;
            float bias = (threshold - 35) / 255f;
            float hollowDepth = asteroidData.HollowCorePercent > 0 && hollowRadius > 0f ? 1f - asteroidData.HollowCorePercent / 100f : 2f;

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

                        int blockId = 0;
                        if (mask != null)
                        {
                            if (mask.IsSolid(gx, gy, gz)) blockId = GetBlockIdFromDistance(shape.Depth(pos, radius));
                        }
                        else
                        {
                            float distance = shape.Distance(pos, radius);
                            float density = -distance - bias;
                            bool solid = density > halfRoughness;
                            if (!solid && density > -halfRoughness)
                            {
                                Vector3 samplePos = (pos + regionSize * 0.5f) * noiseScale;
                                float noise = noiseGenerator.PerlinNoise3D(samplePos.X, samplePos.Y, samplePos.Z, noiseOctaves) / 255f;
                                solid = density + shape.Roughness * (noise - 0.5f) > 0f;
                            }

                            if (solid)
                            {
                                float depth = shape.DepthFromDistance(distance);
                                if (depth <= hollowDepth) blockId = GetBlockIdFromDistance(depth);
                            }
                        }

                        chunkData[x, y, z] = blockId;
                    }
                }
            }

            return chunkData;
        }

        private void EnsureNoise()
        {
            if (noiseGenerator != null) return;

            noiseGenerator = new NoiseGenerator(seed);
            noiseGenerator.Masks.sphericalFalloffDistance = asteroidDimensions.X * 0.5f;
            noiseGenerator.Masks.sphericalGradient = true;
        }
    }
}