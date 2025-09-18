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
    public Biome[] Biomes { get; set; } = new Biome[] { };

    public Dictionary<string, AsteroidData> loadedAsteroids = new Dictionary<string, AsteroidData>();
    public Dictionary<string, Biome> loadedBiomes = new Dictionary<string, Biome>();

    public Biome? GetBiome(string id)
    {
        return loadedBiomes.TryGetValue(id, out Biome biome) ? biome : null;
    }

    public AsteroidData? GetAsteroid(string id)
    {
        return loadedAsteroids.TryGetValue(id, out AsteroidData asteroid) ? asteroid : null;
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
            Debug.Log($"    Asteroids: {biome.Asteroids?.Length ?? 0}");
            if (biome.Asteroids != null)
            {
                foreach (var asteroid in biome.Asteroids)
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
            Debug.Log($"    Size: {asteroid.SizeInChunks} chunks, Threshold: {asteroid.DensityThreshold}");
            Debug.Log($"    Noise: {asteroid.NoiseOctaves} octaves, Scale: {asteroid.NoiseScale}");
            Debug.Log($"    Features: Cavities={asteroid.HasCavities}, Ores={asteroid.HasOreDeposits}, Worms={asteroid.UsePerlinWorms}");
            if (asteroid.WormSettings != null)
            {
                var w = asteroid.WormSettings;
                Debug.Log($"    Worms: Count={w.Count}, Diameter={w.DiameterInBlocks}, Length={w.MaxTunnelLength}");
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

    public Biome(string idString, string name, string description)
    {
        Id = MaxId++;
        IdString = idString;
        Name = name;
        Description = description;

    }

    public Biome(BiomeJSON biomeJSON)
    {
        Id = MaxId++;
        IdString = biomeJSON.Id;
        Name = biomeJSON.Name;
        Description = biomeJSON.Description;
        SpawnChance = biomeJSON.SpawnChance;
        MinDistanceFromCenter = biomeJSON.MinDistanceFromCenter;
        MaxDistanceFromCenter = biomeJSON.MaxDistanceFromCenter;

        Asteroids = new AsteroidData[biomeJSON.AsteroidIds.Length];
       
    }
    public string IdString { get; set; } = "default_biome";
    public string Name { get; set; } = "Default Biome";
    public string Description { get; set; } = "A basic asteroid field";
    public byte SpawnChance { get; set; } = 100;
    public int MinDistanceFromCenter { get; set; } = 100;
    public int MaxDistanceFromCenter { get; set; } = 500;
    public AsteroidData[] Asteroids { get; set; }
}

public class AsteroidData
{
    public string IdString { get; set; } = "default";

    private static byte MaxId = 0;
    public byte Id { get; private set; }

    public static void Reset() { MaxId = 0; }

    public AsteroidData(string idString, string name)
    {
        Id = MaxId++;
        IdString = idString;
        Name = name;
    }

    public string Name { get; set; } = "Default Asteroid";
    public byte SizeInChunks { get; set; } = 2;
    public int DensityThreshold { get; set; } = 33;
    public byte NoiseOctaves { get; set; } = 3;
    public float NoiseScale { get; set; } = 1f;

    public bool HasCavities { get; set; } = false;
    public bool HasOreDeposits { get; set; } = false;
    public bool UsePerlinWorms { get; set; } = false;

    public WormSettingsJSON? WormSettings { get; set; }

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
    public byte SpawnChance { get; set; } = 100;
    public byte MinVeinSize { get; set; } = 1;
    public byte MaxVeinSize { get; set; } = 5;
    public bool CanSpawnNearVoid { get; set; } = true;
}
