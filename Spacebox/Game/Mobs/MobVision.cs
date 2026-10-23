namespace Spacebox.Game.Mobs;

public static class MobVision
{
    public static float NoticeRange(float sightRange, float darkSightRange, bool targetHasLight, bool provoked)
    {
        if (darkSightRange <= 0f || targetHasLight || provoked) return sightRange;
        return MathF.Min(sightRange, darkSightRange);
    }

    public static bool RollWatch(float watchChance, Random random)
    {
        return watchChance > 0f && random.NextDouble() < watchChance;
    }
}
