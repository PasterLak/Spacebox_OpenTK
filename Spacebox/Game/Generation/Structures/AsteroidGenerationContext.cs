namespace Spacebox.Game.Generation.Structures;

public sealed class AsteroidGenerationContext
{
    public static readonly AsteroidGenerationContext Legacy = new AsteroidGenerationContext(GeneratorVersions.Legacy, null, 0f, 0);

    public AsteroidGenerationContext(int version, Biome biome, float distanceFromCenter, int radius)
    {
        Version = GeneratorVersions.Normalize(version);
        Biome = biome;
        DistanceFromCenter = distanceFromCenter;
        Radius = radius;
    }

    public int Version { get; }
    public Biome Biome { get; }
    public float DistanceFromCenter { get; }
    public int Radius { get; }
    public bool UsesRules => GeneratorVersions.UsesRules(Version);

    public static AsteroidGenerationContext From(NotGeneratedEntity data)
    {
        if (data == null || !GeneratorVersions.UsesRules(data.generatorVersion)) return Legacy;
        return new AsteroidGenerationContext(data.generatorVersion, data.biome, data.generationDistance, data.radiusBlocks);
    }

    public WormSettingsJSON WormsFor(AsteroidData asteroid)
    {
        return UsesRules && Biome?.WormSettings != null ? Biome.WormSettings : asteroid.WormSettings;
    }

    public AsteroidCavitiesJSON CavitiesFor(AsteroidData asteroid)
    {
        if (!UsesRules) return null;
        return asteroid.Cavities ?? (asteroid.HasCavities ? AsteroidCavities.Default : null);
    }
}
