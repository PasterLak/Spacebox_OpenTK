using Engine;

namespace Spacebox.Game.Generation.Structures;


public class GeneratorJSON
{
    public string Version { get; set; } = "1";
    public float NoiseModifier { get; set; } = 1;
    public int MinAsteroidsInSector { get; set; } = 50;
    public int MaxAsteroidsInSector { get; set; } = 200;
    public int RejectionSamples { get; set; } = 5;
    public int MinDistanceBetweenAsteroids { get; set; } = 128;
    public float BiomesMapNoiseFrequency { get; set; } = 0.02f;
    public string[] Biomes { get; set; } = new string[] { };
}

public class BiomeJSON
{
    public string Id { get; set; } = "default_biome";
    public string Name { get; set; } = "Default Biome";
    public string Description { get; set; } = "A basic asteroid field";
    public byte SpawnChance { get; set; } = 100;
    public Color3Byte DebugColor { get; set; } = Color3Byte.Pink;
    public int MinDistanceFromCenter { get; set; } = 100;
    public int MaxDistanceFromCenter { get; set; } = 500;
   
    public Dictionary<string, byte> Asteroids { get; set; } = new Dictionary<string, byte>();
}

public class AsteroidJSON
{
    public string Id { get; set; } = "default";
    public string Name { get; set; } = "Default Asteroid";
   
    public byte SizeInChunks { get; set; } = 2;
    public int DensityThreshold { get; set; } = 33;
    public byte NoiseOctaves { get; set; } = 3;
    public float NoiseScale { get; set; } = 1f;

    public bool HasCavities { get; set; } = false;
    public bool HasOreDeposits { get; set; } = false;
    public bool UsePerlinWorms { get; set; } = false;

    public WormSettingsJSON? WormSettings { get; set; }

    public AsteroidLayerJSON[] Layers { get; set; } = new AsteroidLayerJSON[0];

}

public class WormSettingsJSON  
{
    public byte MinCount { get; set; } = 1;
    public byte MaxCount { get; set; } = 3;
    public byte DiameterInBlocks { get; set; } = 3;
    public byte MaxTunnelLength { get; set; } = 3; 
    public float PathDeviation { get; set; } = 1f; // 0 strenght line, 1 360 grad
    public float StepSize { get; set; } = 1f; // // 1=  fast, big steps| 0 = slow, smooth steps
}

public class AsteroidLayerJSON
{
    public string Name { get; set; } = "BaseLayer";
    public string FillBlockID { get; set; } = "default";
    public byte Ratio { get; set; } = 100;
    public AsteroidVeinJSON[] Veins { get; set; } = new AsteroidVeinJSON[0];
}

public class AsteroidVeinJSON
{
    public string BlockId { get; set; } = "default";
    public int SpawnChance { get; set; } = 100000;
    public byte MinVeinSize { get; set; } = 1;
    public byte MaxVeinSize { get; set; } = 5;
    public bool CanSpawnNearVoid { get; set; } = true;
}
