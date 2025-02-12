
using OpenTK.Mathematics;
using Engine.Utils;

public struct WormParameters
{
    public byte WormCount;
    public byte WormDiameter;
    public float Deviation;
    public byte MaxDistance;
    public float StepSize;
    public int Seed;

    public WormParameters(byte wormCount, byte wormDiameter, float deviation, byte maxDistance, float stepSize, int seed)
    {
        WormCount = wormCount;
        WormDiameter = wormDiameter;
        Deviation = deviation;
        MaxDistance = maxDistance;
        StepSize = stepSize;
        Seed = seed;
    }
}

public class PerlinWorms
{
    private WormParameters parameters;

    public PerlinWorms(WormParameters parameters)
    {
        this.parameters = parameters;
    }

    public void CarveWorms(int[,,] voxels, float blockSize)
    {
        int gridSize = voxels.GetLength(0);
        Random random = new Random(parameters.Seed);
        float radius = (parameters.WormDiameter / blockSize) * 0.5f;
        int r = (int)Math.Ceiling(radius);
        float radiusSqr = radius * radius;
        List<Vector3i> sphereOffsets = new List<Vector3i>();
        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    if (dx * dx + dy * dy + dz * dz <= radiusSqr)
                    {
                        sphereOffsets.Add(new Vector3i(dx, dy, dz));
                    }
                }
            }
        }
        float noiseScale = 0.1f;
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        for (int i = 0; i < parameters.WormCount; i++)
        {
            Vector3 wormPos = new Vector3(random.Next(0, gridSize), random.Next(0, gridSize), random.Next(0, gridSize));
            Vector3 direction = RandomUnitVector(random);
            float traveled = 0f;
            while (traveled < parameters.MaxDistance / parameters.StepSize)
            {
                int baseX = (int)Math.Round(wormPos.X);
                int baseY = (int)Math.Round(wormPos.Y);
                int baseZ = (int)Math.Round(wormPos.Z);
                for (int j = 0, count = sphereOffsets.Count; j < count; j++)
                {
                    Vector3i offset = sphereOffsets[j];
                    int ix = baseX + offset.X;
                    int iy = baseY + offset.Y;
                    int iz = baseZ + offset.Z;
                    if (ix >= 0 && ix < gridSize && iy >= 0 && iy < gridSize && iz >= 0 && iz < gridSize)
                    {
                        voxels[ix, iy, iz] = 0;
                    }
                }
                float noiseX = noise.GetNoise(wormPos.Y * noiseScale, wormPos.Z * noiseScale) - 0.5f;
                float noiseY = noise.GetNoise(wormPos.X * noiseScale, wormPos.Z * noiseScale) - 0.5f;
                float noiseZ = noise.GetNoise(wormPos.X * noiseScale, wormPos.Y * noiseScale) - 0.5f;
                Vector3 noiseVec = new Vector3(noiseX, noiseY, noiseZ) * parameters.Deviation;
                direction = Vector3.Normalize(direction + noiseVec);
                wormPos += direction * parameters.StepSize;
                traveled += parameters.StepSize;
            }
        }
    }

    private static Vector3 RandomUnitVector(Random random)
    {
        double theta = 2 * Math.PI * random.NextDouble();
        double phi = Math.Acos(2 * random.NextDouble() - 1);
        float x = (float)(Math.Sin(phi) * Math.Cos(theta));
        float y = (float)(Math.Sin(phi) * Math.Sin(theta));
        float z = (float)Math.Cos(phi);
        return new Vector3(x, y, z);
    }
}
