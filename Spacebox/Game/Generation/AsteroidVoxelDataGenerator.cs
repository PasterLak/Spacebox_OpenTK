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

        public AsteroidVoxelDataGenerator(Vector3 asteroidDimensions, float blockSize, AsteroidData asteroidData, int seed)
        {
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
            if (asteroidData.Layers.Length == 0) return;

            int totalRatio = 0;
            foreach (var layer in asteroidData.Layers)
                totalRatio += layer.Ratio;

            if (totalRatio == 0) return;

            layerThresholds = new float[asteroidData.Layers.Length];
            float currentThreshold = 0f;

            for (int i = 0; i < asteroidData.Layers.Length; i++)
            {
                float layerPortion = (float)asteroidData.Layers[i].Ratio / totalRatio;
                currentThreshold += layerPortion;
                layerThresholds[i] = currentThreshold;
            }
        }

        private int GetBlockIdFromDistance(float normalizedDistance)
        {
            if (layerThresholds == null || layerThresholds.Length == 0)
                return 1;

            for (int i = 0; i < layerThresholds.Length; i++)
            {
                if (normalizedDistance <= layerThresholds[i])
                    return asteroidData.Layers[i].FillBlockID;
            }

            return asteroidData.Layers[asteroidData.Layers.Length - 1].FillBlockID;
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

            var chunkData = new int[paddedSize, paddedSize, paddedSize];

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
                        if (noiseValue > threshold)
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