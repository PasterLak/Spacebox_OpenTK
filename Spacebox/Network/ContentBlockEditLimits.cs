using Spacebox.Game;

namespace Spacebox.Network;

public static class ContentBlockEditLimits
{
    private static BlockEditLimits? _cached;

    public static BlockEditLimits Current => _cached ??= Build();

    public static void Reset() => _cached = null;

    private static BlockEditLimits Build()
    {
        int maxTier = 0;
        float? fastestRepair = null;
        foreach (var item in GameAssets.Items.Values)
        {
            if (item is DrillItem drill) maxTier = Math.Max(maxTier, drill.Tier);
            else if (item is RepairToolItem tool) fastestRepair = fastestRepair.HasValue ? MathF.Min(fastestRepair.Value, tool.RepairTime) : tool.RepairTime;
        }

        int maxProjectile = 0;
        foreach (var projectile in GameAssets.Projectiles.Values) maxProjectile = Math.Max(maxProjectile, projectile.DamageBlocks);

        int maxStructure = 0;
        foreach (var block in GameAssets.Blocks.Values)
        {
            if (block == null) continue;
            int cells = Math.Max(1, block.Size.X * block.Size.Y * block.Size.Z);
            maxStructure = Math.Max(maxStructure, block.Durability * cells);
        }

        return BlockEditLimits.Create(maxTier, maxProjectile, fastestRepair, maxStructure);
    }
}
