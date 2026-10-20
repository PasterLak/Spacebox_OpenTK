using OpenTK.Mathematics;

namespace Engine.PostProcessing.Ssao;

public static class SsaoKernel
{
    public const int DefaultSeed = 1337;
    private const float MinScale = 0.1f;

    public static Vector3[] Build(int count, int seed = DefaultSeed)
    {
        if (count <= 0) return Array.Empty<Vector3>();

        var random = new Random(seed);
        var samples = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 direction;
            do
            {
                direction = new Vector3(
                    (float)random.NextDouble() * 2f - 1f,
                    (float)random.NextDouble() * 2f - 1f,
                    (float)random.NextDouble());
            }
            while (direction.LengthSquared < 0.0001f || direction.Z < 0.15f * direction.Length);

            direction.Normalize();
            float t = (i + 1) / (float)count;
            float scale = MinScale + (1f - MinScale) * t * t;
            samples[i] = direction * scale;
        }

        return samples;
    }
}
