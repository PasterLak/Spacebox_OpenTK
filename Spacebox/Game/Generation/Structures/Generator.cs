using Engine;

namespace Spacebox.Game.Generation.Structures;


public class Generator
{
    public string Version { get; set; } = "1";
    public float NoiseModifier { get; set; } = 1;
    public int MinAsteroidsInSector { get; set; } = 50;
    public int MaxAsteroidsInSector { get; set; } = 200;
    public int RejectionSamples { get; set; } = 5;
    public int MinDistanceBetweenAsteroids { get; set; } = 128;
    public float BiomesMapNoiseFrequency { get; set; } = 0.02f;
    public Biome[] Biomes { get; set; } = new Biome[] { };
    public byte[] BiomeProbabilityRanges { get; private set; } = new byte[] { };


    public Dictionary<string, AsteroidData> loadedAsteroids = new Dictionary<string, AsteroidData>();
    public Dictionary<string, Biome> loadedBiomes = new Dictionary<string, Biome>();

    public Biome GetBiomeByRandomValue(byte randomValue)
    {
        for (int i = 0; i < BiomeProbabilityRanges.Length; i++)
        {
            if (randomValue < BiomeProbabilityRanges[i])
            {
                return Biomes[i];
            }
        }
        return Biomes[Biomes.Length - 1];
    }

    public Biome? GetBiome(string id)
    {
        return loadedBiomes.TryGetValue(id, out Biome biome) ? biome : null;
    }

    public AsteroidData? GetAsteroid(string id)
    {
        return loadedAsteroids.TryGetValue(id, out AsteroidData asteroid) ? asteroid : null;
    }

    public void CalculateBiomeProbabilities()
    {
        BiomeProbabilityRanges = CalculateBiomeProbabilityRanges();
    }

    private byte[] CalculateBiomeProbabilityRanges()
    {
        if (Biomes.Length == 0) return new byte[0];

        int totalChance = 0;
        foreach (var biome in Biomes)
        {
            totalChance += biome.SpawnChance;
        }

        if (totalChance == 0) return new byte[0];

        byte[] ranges = new byte[Biomes.Length];
        byte currentRange = 0;

        for (int i = 0; i < Biomes.Length; i++)
        {
            float normalizedChance = (float)Biomes[i].SpawnChance / totalChance;
            byte rangeSize = (byte)(normalizedChance * 255);

            if (i == Biomes.Length - 1)
            {
                rangeSize = (byte)(255 - currentRange);
            }

            currentRange += rangeSize;
            ranges[i] = currentRange;
        }

        return ranges;
    }

    public void DebugPrint()
    {
        Debug.Log("=== GENERATOR DEBUG INFO ===");
        Debug.Log($"Version: {Version}");
        Debug.Log($"NoiseModifier: {NoiseModifier}");
        Debug.Log($"Biomes Count: {Biomes.Length}");
        Debug.Log($"Loaded Asteroids: {loadedAsteroids.Count}");
        Debug.Log($"Loaded Biomes: {loadedBiomes.Count}");

        Debug.Log("\n--- BIOMES ---");
        foreach (var biome in Biomes)
        {
            Debug.Log($"  [{biome.Id}] {biome.IdString} - {biome.Name}");
            Debug.Log($"    Chance: {biome.SpawnChance}%, Distance: {biome.MinDistanceFromCenter}-{biome.MaxDistanceFromCenter}");
            Debug.Log($"    Asteroids: {biome.AsteroidChances.Keys?.Count ?? 0}");
            if (biome.AsteroidChances.Keys != null)
            {
                foreach (var asteroid in biome.AsteroidChances.Keys)
                {
                    if (asteroid != null)
                        Debug.Log($"      - {asteroid.IdString}");
                }
            }
            Debug.Log("");
        }

        Debug.Log("--- LOADED ASTEROIDS ---");
        foreach (var kvp in loadedAsteroids)
        {
            var asteroid = kvp.Value;
            Debug.Log($"  [{asteroid.Id}] {asteroid.IdString} - {asteroid.Name}");
            Debug.Log($"    Radius: {asteroid.MinRadius}-{asteroid.MaxRadius} block, Threshold: {asteroid.DensityThreshold}");
            Debug.Log($"    Noise: {asteroid.NoiseOctaves} octaves, Scale: {asteroid.NoiseScale}");
            Debug.Log($"    Features: Cavities={asteroid.HasCavities}, Ores={asteroid.HasOreDeposits}, Worms={asteroid.UsePerlinWorms}");
            if (asteroid.WormSettings != null)
            {
                var w = asteroid.WormSettings;
                Debug.Log($"    Worms: MinCount={w.MinCount},MaxCount={w.MaxCount}, Diameter={w.DiameterInBlocks}, Length={w.MaxTunnelLength}");
            }
            Debug.Log($"    Layers: {asteroid.Layers.Length}");
            foreach (var layer in asteroid.Layers)
            {
                Debug.Log($"      - {layer.Name}: Block={layer.FillBlockID}, Ratio={layer.Ratio}%, Veins={layer.Veins.Length}");
            }
            Debug.Log("");
        }
    }
}

public class Biome
{
    private static byte MaxId = 0;
    public byte Id { get; private set; }

    public static void Reset() { MaxId = 0; }

    public Biome(BiomeJSON biomeJSON)
    {
        Id = MaxId++;
        IdString = biomeJSON.Id;
        Name = biomeJSON.Name;
        Description = biomeJSON.Description;
        SpawnChance = biomeJSON.SpawnChance;
        MinDistanceFromCenter = biomeJSON.MinDistanceFromCenter;
        MaxDistanceFromCenter = biomeJSON.MaxDistanceFromCenter;
        DebugColor = biomeJSON.DebugColor;

        if (DebugColor == Color3Byte.Pink)
        {
            DebugColor = Color3Byte.RandomColor(100,255);
        }
     
       
    }
    public string IdString { get; private set; } = "default_biome";
    public string Name { get; private set; } = "Default Biome";
    public string Description { get; private set; } = "A basic asteroid field";
    public Color3Byte DebugColor { get; private set; } = Color3Byte.Pink;
    public byte SpawnChance { get; private set; } = 100;
    public int MinDistanceFromCenter { get; private set; } = 100;
    public int MaxDistanceFromCenter { get; private set; } = 500;
    public Dictionary<AsteroidData, byte> AsteroidChances { get; private set; } = new Dictionary<AsteroidData, byte>();


    public static AsteroidData SelectAsteroidBySpawnChance(Dictionary<AsteroidData, byte> asteroidChances, Random random)
    {
        if (asteroidChances == null || asteroidChances.Count == 0) return null;

        int totalChance = 0;
        foreach (var chance in asteroidChances.Values)
        {
            totalChance += chance;
        }

        if (totalChance == 0)
        {
            var asteroids = asteroidChances.Keys.ToArray();
            return asteroids[random.Next(asteroids.Length)];
        }

        int randomValue = random.Next(totalChance);
        int currentSum = 0;

        foreach (var kvp in asteroidChances)
        {
            currentSum += kvp.Value;
            if (randomValue < currentSum)
                return kvp.Key;
        }

        return asteroidChances.Keys.Last();
    }
}

public class AsteroidData
{
    private static byte MaxId = 0;
    public byte Id { get; private set; }

    public string IdString { get; set; } = "default";
    public byte SpawnChance { get; set; } = 100;


    public static void Reset() { MaxId = 0; }

    public AsteroidData(string idString, string name)
    {
        Id = MaxId++;
        IdString = idString;
        Name = name;
    }

    public string Name { get; set; } = "Default Asteroid";
    public int MinRadius { get; set; } = 2;
    public int MaxRadius { get; set; } = 2;
    public int DensityThreshold { get; set; } = 33;
    public byte NoiseOctaves { get; set; } = 3;
    public float NoiseScale { get; set; } = 1f;

    public bool HasCavities { get; set; } = false;
    public bool HasOreDeposits { get; set; } = false;
    public bool UsePerlinWorms { get; set; } = false;

    public WormSettingsJSON WormSettings { get; set; } = new WormSettingsJSON();

    public AsteroidLayer[] Layers { get; set; } = new AsteroidLayer[0];

}


public class AsteroidLayer
{
    public string Name { get; set; } = "BaseLayer";
    public short FillBlockID { get; set; } = 1;
    public byte Ratio { get; set; } = 100;
    public AsteroidVein[] Veins { get; set; } = new AsteroidVein[0];
}

public class AsteroidVein
{
    public short BlockId { get; set; } = 0;
    public int SpawnChance { get; set; } = 100000;
    public byte MinVeinSize { get; set; } = 1;
    public byte MaxVeinSize { get; set; } = 5;
    public bool CanSpawnNearVoid { get; set; } = true;
}
