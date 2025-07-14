
using OpenTK.Mathematics;
using Engine.Utils;
using Engine;

public struct WormParameters
{
    public byte WormCount;
    public byte WormDiameter;
    public float Deviation; // 0 strenght line, 1 360 grad
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
    readonly WormParameters p;
    readonly List<Vector3i> sphereOffsets;

    public PerlinWorms(WormParameters parameters)
    {
        p = parameters;
        sphereOffsets = BuildSphereOffsets(1);
    }

    static readonly Dictionary<(int d, int c), Vector3i[]> Cache = new();



    public static IEnumerable<(Vector3SByte chunk, Vector3i voxel)> Voxels(WormParameters p, int chunkCount, int chunkSize)
    {
        int world = chunkCount * chunkSize;
        int half = chunkCount / 2;

        var rng = new Random(p.Seed);
        var noise = new FastNoiseLite(p.Seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        float step = MathF.Max(1f, p.StepSize);
        int maxSteps = (int)(p.MaxDistance / step);
        var off = Offsets(p.WormDiameter);

        for (int w = 0; w < p.WormCount; ++w)
        {
            var pos = new Vector3(rng.Next(world), rng.Next(world), rng.Next(world));
            var dir = Rand(rng);

            for (int s = 0; s < maxSteps; ++s)
            {
                int bx = (int)MathF.Round(pos.X);
                int by = (int)MathF.Round(pos.Y);
                int bz = (int)MathF.Round(pos.Z);

                foreach (var o in off)
                {
                    int vx = bx + o.X, vy = by + o.Y, vz = bz + o.Z;
                    if ((uint)vx >= world || (uint)vy >= world || (uint)vz >= world) continue;

                    var c = new Vector3SByte(
                        (sbyte)(vx / chunkSize - half),
                        (sbyte)(vy / chunkSize - half),
                        (sbyte)(vz / chunkSize - half));

                    var v = new Vector3i(vx % chunkSize, vy % chunkSize, vz % chunkSize);
                    yield return (c, v);
                }

                var n = new Vector3(noise.GetNoise(pos.Y, pos.Z),
                                    noise.GetNoise(pos.X, pos.Z),
                                    noise.GetNoise(pos.X, pos.Y)) - new Vector3(.5f);
                dir = Vector3.Normalize(dir + n * p.Deviation);
                pos += dir * step;
            }
        }
    }

    public void Carve(int[,,] voxels, float blockSize)
    {
        int N = voxels.GetLength(0);
        var rng = new Random(p.Seed);               // RNG is now always seed-locked
        var noise = new FastNoiseLite(p.Seed);        // noise seeded too
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        const float scale = 0.1f;

        for (int w = 0; w < p.WormCount; ++w)
        {
            var pos = new Vector3(
                rng.NextSingle() * (N - 1),
                rng.NextSingle() * (N - 1),
                rng.NextSingle() * (N - 1));
            var dir = RandomUnit(rng);

            float travelled = 0;
            float maxSteps = p.MaxDistance / p.StepSize;

            while (travelled < maxSteps)
            {
                int bx = (int)MathF.Round(pos.X);
                int by = (int)MathF.Round(pos.Y);
                int bz = (int)MathF.Round(pos.Z);

                foreach (var off in sphereOffsets)
                {
                    int x = bx + off.X, y = by + off.Y, z = bz + off.Z;
                    if ((uint)x < N && (uint)y < N && (uint)z < N)
                        voxels[x, y, z] = 0;
                }

                var n = new Vector3(
                    noise.GetNoise(pos.Y * scale, pos.Z * scale),
                    noise.GetNoise(pos.X * scale, pos.Z * scale),
                    noise.GetNoise(pos.X * scale, pos.Y * scale)) - new Vector3(0.5f);
                dir = Vector3.Normalize(dir + n * p.Deviation);

                pos += dir * p.StepSize;
                travelled += p.StepSize;
            }
        }
    }

    // ---- local helpers --------------------------------------------------
    private List<Vector3i> BuildSphereOffsets(int blockSize)
    {
        float rWorld = (p.WormDiameter / blockSize) * 0.5f;
        int rGrid = (int)MathF.Ceiling(rWorld);
        float r2 = rWorld * rWorld;

        var list = new List<Vector3i>();
        for (int dx = -rGrid; dx <= rGrid; ++dx)
            for (int dy = -rGrid; dy <= rGrid; ++dy)
                for (int dz = -rGrid; dz <= rGrid; ++dz)
                    if (dx * dx + dy * dy + dz * dz <= r2)
                        list.Add(new Vector3i(dx, dy, dz));
        return list;
    }
    static Vector3i[] Offsets(int dia)
    {
        float rad = dia * .5f;
        int r = (int)MathF.Ceiling(rad);
        float r2 = rad * rad;
        var list = new List<Vector3i>(r * r * r);
        for (int x = -r; x <= r; ++x)
            for (int y = -r; y <= r; ++y)
                for (int z = -r; z <= r; ++z)
                    if (x * x + y * y + z * z <= r2)
                        list.Add(new Vector3i(x, y, z));
        return list.ToArray();
    }

    static Vector3 Rand(Random r)
    {
        double a = 2 * Math.PI * r.NextDouble();
        double b = Math.Acos(2 * r.NextDouble() - 1);
        return new Vector3((float)(Math.Sin(b) * Math.Cos(a)),
                           (float)(Math.Sin(b) * Math.Sin(a)),
                           (float)Math.Cos(b));
    }

    static Vector3 RandomUnit(Random rng)
    {
        double u = rng.NextDouble();
        double v = rng.NextDouble();
        double θ = 2 * Math.PI * u;
        double φ = Math.Acos(2 * v - 1);
        return new((float)(Math.Sin(φ) * Math.Cos(θ)),
                    (float)(Math.Sin(φ) * Math.Sin(θ)),
                    (float)Math.Cos(φ));
    }

}
