namespace Spacebox.Game.Player;

public static class ArmorProtection
{
    public const float MaxExplosionProtection = 0.75f;

    public static float FromPercent(float percent)
    {
        if (float.IsNaN(percent)) return 0f;
        return Math.Clamp(percent, 0f, 100f) / 100f;
    }

    public static float Combine(float total)
    {
        return Math.Clamp(total, 0f, MaxExplosionProtection);
    }

    public static int Apply(int damage, float protection)
    {
        if (damage <= 0) return 0;

        float reduced = damage * (1f - Math.Clamp(protection, 0f, 1f));
        return (int)MathF.Round(reduced, MidpointRounding.AwayFromZero);
    }
}
