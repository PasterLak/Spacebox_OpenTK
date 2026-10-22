using Engine.Utils;
using Spacebox.Game.Generation.Shapes.Asteroids;

namespace Spacebox.Game.Generation.Structures;

public sealed class AsteroidGenerationContext
{
    public static readonly AsteroidGenerationContext Legacy = new AsteroidGenerationContext(GeneratorVersions.Legacy, null, 0f, 0);

    public AsteroidGenerationContext(int version, Biome biome, float distanceFromCenter, int radius, AsteroidShape shape = null)
    {
        Version = GeneratorVersions.Normalize(version);
        Biome = biome;
        DistanceFromCenter = distanceFromCenter;
        Radius = radius;
        Shape = GeneratorVersions.UsesShapes(Version) ? shape : null;
    }

    public int Version { get; }
    public Biome Biome { get; }
    public float DistanceFromCenter { get; }
    public int Radius { get; }
    public AsteroidShape Shape { get; }
    public bool UsesRules => GeneratorVersions.UsesRules(Version);
    public bool RemovesFloatingBlocks => Shape != null && Shape.RemoveFloatingBlocks;

    public static AsteroidShape CreateShape(int version, AsteroidData asteroid, int seed)
    {
        if (!GeneratorVersions.UsesShapes(version) || asteroid?.Shape == null) return null;
        if (new Random(seed ^ 0x6A09E667).Next(100) >= asteroid.Shape.Chance) return null;
        return new AsteroidShape(asteroid.Shape, seed);
    }

    public float DepthAt(OpenTK.Mathematics.Vector3 local)
    {
        return Shape != null ? Shape.Depth(local, Radius) : AsteroidLayers.Depth(local.Length, Radius);
    }

    public static AsteroidGenerationContext From(NotGeneratedEntity data)
    {
        if (data == null || !GeneratorVersions.UsesRules(data.generatorVersion)) return Legacy;
        var shape = CreateShape(data.generatorVersion, data.asteroid, SeedHelper.ToIntSeed(data.Id));
        return new AsteroidGenerationContext(data.generatorVersion, data.biome, data.generationDistance, data.radiusBlocks, shape);
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
