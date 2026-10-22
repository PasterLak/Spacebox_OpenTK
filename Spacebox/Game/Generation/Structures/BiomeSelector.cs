namespace Spacebox.Game.Generation.Structures;

public static class BiomeSelector
{
    public static Biome Select(Biome[] biomes, byte noiseValue, float distanceFromCenter, Biome fallback)
    {
        int total = 0;
        for (int i = 0; i < biomes.Length; i++)
        {
            var biome = biomes[i];
            if (biome != null && biome.Distance.Contains(distanceFromCenter)) total += biome.SpawnChance;
        }

        if (total == 0) return fallback;

        int target = noiseValue * total / 256;
        int sum = 0;
        Biome last = null;
        for (int i = 0; i < biomes.Length; i++)
        {
            var biome = biomes[i];
            if (biome == null || biome.SpawnChance == 0 || !biome.Distance.Contains(distanceFromCenter)) continue;

            sum += biome.SpawnChance;
            last = biome;
            if (target < sum) return biome;
        }

        return last ?? fallback;
    }
}
