namespace Spacebox.Game.Generation.Blocks;

public static class DamageStage
{
    public const int Count = 4;
    public const int Max = Count - 1;

    public static int Of(int durability, int maxDurability)
    {
        if (maxDurability <= 0 || durability >= maxDurability) return 0;

        float lost = 1f - Math.Max(0, durability) / (float)maxDurability;
        return Math.Clamp(1 + (int)(lost * Max), 1, Max);
    }

    public static bool Changed(int before, int after, int maxDurability) => Of(before, maxDurability) != Of(after, maxDurability);
}
