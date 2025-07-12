using OpenTK.Mathematics;
using Spacebox.Generation;

namespace Spacebox.Game.Generation
{
    public enum AsteroidType : byte { Light, Medium, Heavy }

    public class AsteroidVoxelDataGenerator
    {
        public int[,,] voxelData;
        public int gridSize;
        public float blockSize;
        public float asteroidDiameter;
        public int threshold;
        public byte noiseOctaves;
        public float noiseScale;
        public int seed;
        private NoiseGenerator noiseGenerator;
        public AsteroidType type;
        public int[,,] VoxelData => voxelData;

        public AsteroidVoxelDataGenerator(
            float asteroidDiameter,
            float blockSize,
            int threshold,
            byte noiseOctaves,
            float noiseScale,
            int seed,
            AsteroidType type
        )
        {
            this.type = type;
            this.asteroidDiameter = asteroidDiameter;
            this.blockSize = blockSize;
            this.threshold = threshold;
            this.noiseOctaves = noiseOctaves;
            this.noiseScale = noiseScale;
            this.seed = seed;
        }

        public void GenerateData()
        {
            noiseGenerator = new NoiseGenerator(seed);
            noiseGenerator.Masks.sphericalFalloffDistance = asteroidDiameter * 0.5f;
            noiseGenerator.Masks.sphericalGradient = true;
            gridSize = (int)System.MathF.Ceiling(asteroidDiameter / blockSize);
            if (gridSize % 2 == 0) gridSize++;
            int half = gridSize / 2;
            voxelData = new int[gridSize, gridSize, gridSize];
            Vector3 regionSize = new Vector3(asteroidDiameter, asteroidDiameter, asteroidDiameter);
            Vector3 noiseOffset = regionSize * 0.5f;

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
                            float radius = asteroidDiameter * 0.5f;
                            float normalized = distance / radius;
                            int blockId;
                            if (type == AsteroidType.Light)
                            {
                                if (normalized >= 0.4f) blockId = 1; else blockId = 2;
                            }
                            else if (type == AsteroidType.Medium)
                            {
                                if (normalized >= 0.6f) blockId = 1; else if (normalized >= 0.3f) blockId = 2; else blockId = 3;
                            }
                            else
                            {
                                if (normalized >= 0.7f) blockId = 1; else if (normalized >= 0.4f) blockId = 2; else blockId = 3;
                            }
                            voxelData[ix, iy, iz] = blockId;
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
