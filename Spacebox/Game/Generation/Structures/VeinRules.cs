namespace Spacebox.Game.Generation.Structures;

public static class VeinRules
{
    public static bool Allows(AsteroidVein vein, float depth, float distanceFromCenter)
    {
        return depth >= vein.MinDepth && depth <= vein.MaxDepth && vein.Distance.Contains(distanceFromCenter);
    }

    public static int Chance(AsteroidVein vein, Biome biome)
    {
        if (biome == null) return vein.SpawnChance;

        double chance = vein.SpawnChance * (double)biome.OreMultiplier;
        if (vein.BiomeMultipliers.TryGetValue(biome.IdString, out int percent)) chance = chance * Math.Max(0, percent) / 100.0;
        return (int)Math.Min(1_000_000, Math.Round(chance));
    }
}
