using Engine;
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
        public Vector3 asteroidDimensions;
        public int threshold;
        public byte noiseOctaves;
        public float noiseScale;
        public int seed;
        private NoiseGenerator noiseGenerator;
        public AsteroidType type;
        public int[,,] VoxelData => voxelData;

        public AsteroidVoxelDataGenerator(
            Vector3 asteroidDimensions,
            float blockSize,
            int threshold,
            byte noiseOctaves,
            float noiseScale,
            int seed,
            AsteroidType type
        )
        {
            this.type = type;
            this.asteroidDimensions = asteroidDimensions;
            this.blockSize = blockSize;
            this.threshold = threshold;
            this.noiseOctaves = noiseOctaves;
            this.noiseScale = noiseScale;
            this.seed = seed;
        }

        public int[,,] GenerateDataForChunk(Vector3SByte chunkIdx, int chunkSize)
        {
            if (noiseGenerator == null)
            {
                noiseGenerator = new NoiseGenerator(seed);
                noiseGenerator.Masks.sphericalFalloffDistance =  asteroidDimensions.X * 0.5f;
                noiseGenerator.Masks.sphericalGradient = true;
            }

            gridSize = (int)MathF.Ceiling(asteroidDimensions.X / blockSize);
            if (gridSize % 2 == 0) gridSize++;

            Vector3 regionSize = asteroidDimensions;
            Vector3 noiseOffset = regionSize * 0.5f;
            float radius = asteroidDimensions.X * 0.5f ;

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

                        /*byte noiseValue = noiseGenerator.PerlinNoise3D(
                           samplePos.X, samplePos.Y, samplePos.Z,
                           //  ref regionSize,
                           noiseOctaves
                       // noiseGenerator.Masks.SphericalMaskFunction
                       );*/

                        int blockId;
                        if (noiseValue > threshold)
                        {
                            float distance = pos.Length;
                           // float radius = asteroidDimensions.X * 0.5f;
                            float norm = distance / radius  * 100;

                            if (type == AsteroidType.Light)
                                blockId = norm >= 40 ? 1 : 2;
                            else if (type == AsteroidType.Medium)
                                blockId = norm >= 60 ? 1 : (norm >= 30 ? 2 : 3);
                            else
                                blockId = norm >= 70 ? 1 : (norm >= 40 ? 2 : 3);
                        }
                        else
                        {
                            blockId = 0;
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
            gridSize = (int)System.MathF.Ceiling(asteroidDimensions.X / blockSize);
            if (gridSize % 2 == 0) gridSize++;
            int half = gridSize / 2;
            voxelData = new int[gridSize, gridSize, gridSize];
            Vector3 regionSize = asteroidDimensions;
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
                            float radius = asteroidDimensions.X * 0.5f;
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
