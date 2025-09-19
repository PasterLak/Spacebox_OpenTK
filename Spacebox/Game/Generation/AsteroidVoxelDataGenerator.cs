using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;
using Spacebox.Generation;

namespace Spacebox.Game.Generation
{
    public class AsteroidVoxelDataGenerator
    {
        public int[,,] voxelData;
        public int gridSize;
        public float blockSize;
        public Vector3 asteroidDimensions;
        public int threshold;
        public byte noiseOctaves;
        public float noiseScale;
        public int seed;
        private NoiseGenerator noiseGenerator;
        private AsteroidData asteroidData;
        private float[] layerThresholds;

        public int[,,] VoxelData => voxelData;

        public AsteroidVoxelDataGenerator(
            Vector3 asteroidDimensions,
            float blockSize,
            AsteroidData asteroidData,
            int seed
        )
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

        public int[,,] GenerateDataForChunk(Vector3SByte chunkIdx)
        {
            var chunkSize = Chunk.Size;
            if (noiseGenerator == null)
            {
                noiseGenerator = new NoiseGenerator(seed);
                noiseGenerator.Masks.sphericalFalloffDistance = asteroidDimensions.X * 0.5f;
                noiseGenerator.Masks.sphericalGradient = true;
            }

            gridSize = (int)MathF.Ceiling(asteroidDimensions.X / blockSize);
            if (gridSize % 2 == 0) gridSize++;

            Vector3 regionSize = asteroidDimensions;
            Vector3 noiseOffset = regionSize * 0.5f;
            float radius = asteroidDimensions.X * 0.5f;

            var chunkData = new int[chunkSize, chunkSize, chunkSize];

            for (int x = 0; x < chunkSize; x++)
                for (int y = 0; y < chunkSize; y++)
                    for (int z = 0; z < chunkSize; z++)
                    {
                        int gx = chunkIdx.X * chunkSize + x;
                        int gy = chunkIdx.Y * chunkSize + y;
                        int gz = chunkIdx.Z * chunkSize + z;

                        Vector3 pos = new Vector3(gx, gy, gz) * blockSize;
                        Vector3 samplePos = (pos + noiseOffset) * noiseScale;

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

            return chunkData;
        }

        public void GenerateData()
        {
            noiseGenerator = new NoiseGenerator(seed);
            noiseGenerator.Masks.sphericalFalloffDistance = asteroidDimensions.X * 0.5f;
            noiseGenerator.Masks.sphericalGradient = true;
            gridSize = (int)MathF.Ceiling(asteroidDimensions.X / blockSize);
            if (gridSize % 2 == 0) gridSize++;
            int half = gridSize / 2;
            voxelData = new int[gridSize, gridSize, gridSize];
            Vector3 regionSize = asteroidDimensions;
            Vector3 noiseOffset = regionSize * 0.5f;
            float radius = asteroidDimensions.X * 0.5f;

            for (int x = -half; x <= half; x++)
            {
                for (int y = -half; y <= half; y++)
                {
                    for (int z = -half; z <= half; z++)
                    {
                        Vector3 pos = new Vector3(x, y, z) * blockSize;
                        Vector3 samplePos = (pos + noiseOffset) * noiseScale;
                        byte noiseValue = noiseGenerator.PerlinNoise3DWithMask(samplePos, ref regionSize, noiseOctaves, noiseGenerator.Masks.SphericalMaskFunction);

                        int ix = x + half;
                        int iy = y + half;
                        int iz = z + half;

                        if (noiseValue > threshold)
                        {
                            float distance = pos.Length;
                            float normalized = 1.0f - (distance / radius);
                            normalized = MathF.Max(0f, MathF.Min(1f, normalized));
                            voxelData[ix, iy, iz] = GetBlockIdFromDistance(normalized);
                        }
                        else
                        {
                            voxelData[ix, iy, iz] = 0;
                        }
                    }
                }
            }
        }
    }
}