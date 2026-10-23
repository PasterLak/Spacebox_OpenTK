namespace Spacebox.Network;

public readonly struct BlockEditLimits
{
    public const float MaxRepairStepSeconds = 0.25f;
    public const float MinRepairTime = 0.01f;

    public BlockEditLimits(byte maxDamage, byte maxRepair)
    {
        MaxDamage = maxDamage;
        MaxRepair = maxRepair;
    }

    public byte MaxDamage { get; }
    public byte MaxRepair { get; }

    public static BlockEditLimits Create(int maxDrillTier, int maxProjectileDamage, float? fastestRepairTime, int maxStructureDurability)
    {
        int damage = Math.Clamp(Math.Max(maxDrillTier, maxProjectileDamage), 1, byte.MaxValue);
        int repair = 0;
        if (fastestRepairTime.HasValue && maxStructureDurability > 0)
        {
            float rate = maxStructureDurability / MathF.Max(fastestRepairTime.Value, MinRepairTime);
            repair = Math.Clamp((int)MathF.Ceiling(rate * MaxRepairStepSeconds), 1, byte.MaxValue);
        }

        return new BlockEditLimits((byte)damage, (byte)repair);
    }

    public byte ClampDamage(byte requested, bool isTransparent, byte blockMaxDurability)
    {
        int limit = isTransparent ? Math.Max(MaxDamage, blockMaxDurability) : MaxDamage;
        return (byte)Math.Min(requested, limit);
    }

    public byte ClampRepair(byte requested) => Math.Min(requested, MaxRepair);
}
