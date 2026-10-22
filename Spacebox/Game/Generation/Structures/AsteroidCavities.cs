using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Structures;

public static class AsteroidCavities
{
    public static readonly AsteroidCavitiesJSON Default = new AsteroidCavitiesJSON { MinCount = 1, MaxCount = 3, MinRadius = 3, MaxRadius = 6 };

    private const float PlacementShare = 0.7f;

    public static List<(Vector3 Center, float Radius)> Create(int seed, float asteroidRadius, AsteroidCavitiesJSON settings)
    {
        var spheres = new List<(Vector3, float)>();
        if (settings == null || settings.MaxCount == 0) return spheres;

        var random = new Random(seed ^ 0x51C4A7E3);
        int count = random.Next(settings.MinCount, settings.MaxCount + 1);
        for (int i = 0; i < count; i++)
        {
            float radius = random.Next(settings.MinRadius, settings.MaxRadius + 1);
            float reach = MathF.Max(0f, asteroidRadius * PlacementShare - radius);
            spheres.Add((RandomPointInSphere(random) * reach, radius));
        }
        return spheres;
    }

    public static void Carve(int[,,] padded, Vector3SByte chunkIndex, List<(Vector3 Center, float Radius)> spheres)
    {
        int size = padded.GetLength(0);
        var origin = new Vector3(chunkIndex.X * Chunk.Size - 1, chunkIndex.Y * Chunk.Size - 1, chunkIndex.Z * Chunk.Size - 1);

        foreach (var (center, radius) in spheres)
        {
            var min = Vector3.ComponentMax(center - new Vector3(radius) - origin, Vector3.Zero);
            var max = Vector3.ComponentMin(center + new Vector3(radius) - origin, new Vector3(size - 1));
            if (min.X > max.X || min.Y > max.Y || min.Z > max.Z) continue;

            float radius2 = radius * radius;
            for (int x = (int)min.X; x <= (int)max.X; x++)
            for (int y = (int)min.Y; y <= (int)max.Y; y++)
            for (int z = (int)min.Z; z <= (int)max.Z; z++)
            {
                if ((origin + new Vector3(x, y, z) - center).LengthSquared <= radius2) padded[x, y, z] = 0;
            }
        }
    }

    private static Vector3 RandomPointInSphere(Random random)
    {
        while (true)
        {
            var point = new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f);
            if (point.LengthSquared <= 1f) return point;
        }
    }
}
