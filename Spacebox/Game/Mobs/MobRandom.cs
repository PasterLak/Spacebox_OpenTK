using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs;

public static class MobRandom
{
    public static Vector3 Direction(Random random)
    {
        var direction = new Vector3(
            (float)random.NextDouble() * 2f - 1f,
            (float)random.NextDouble() * 2f - 1f,
            (float)random.NextDouble() * 2f - 1f);

        return direction.LengthSquared > 1e-4f ? direction.Normalized() : Vector3.UnitZ;
    }

    public static float Range(Random random, float min, float max)
    {
        return min + (max - min) * (float)random.NextDouble();
    }

    public static bool Chance(Random random, float probability)
    {
        return random.NextDouble() < probability;
    }
}
