using System.Text.Json;
using Engine;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.GUI;
using Xunit;

namespace Spacebox.Tests.Generation;

public class GenerationRulesTests
{
    private static Biome MakeBiome(string id, byte chance, int min = 0, int max = 0, int density = 100, int spacing = 0)
    {
        return new Biome(new BiomeJSON { Id = id, SpawnChance = chance, MinDistanceFromCenter = min, MaxDistanceFromCenter = max, Density = density, MinDistanceBetweenAsteroids = spacing });
    }

    private static AsteroidData MakeAsteroid(string id, int minRadius = 10, int maxRadius = 20, int minDistance = 0, int maxDistance = 0)
    {
        return new AsteroidData(id, id) { MinRadius = minRadius, MaxRadius = maxRadius, Distance = new DistanceRange(minDistance, maxDistance) };
    }

    private static WorldGenerator MakeGenerator(params Biome[] biomes)
    {
        return new WorldGenerator { MinAsteroidsInSector = 80, MaxAsteroidsInSector = 120, RejectionSamples = 5, MinDistanceBetweenAsteroids = 128, Biomes = biomes };
    }

    private static List<NotGeneratedEntity> Plan(WorldGenerator generator, Biome biome, long seed = 12345)
    {
        return SectorAsteroidPlanner.Plan(generator, seed, new Vector3(8192, 0, 0), _ => biome, p => SeedHelper.GetAsteroidId(seed, p) & 0x7FFFFFFFFFFFFFFF, GeneratorVersions.Rules);
    }

    [Fact]
    public void DistanceRange_ZeroMaxMeansUnlimited()
    {
        Assert.True(new DistanceRange(0, 0).Contains(1e9f));
        Assert.True(new DistanceRange(0, int.MaxValue).IsUnlimited);
        Assert.False(new DistanceRange(100, 200).Contains(99));
        Assert.True(new DistanceRange(100, 200).Contains(200));
        Assert.False(new DistanceRange(100, 200).Contains(201));
    }

    [Fact]
    public void BiomeSelector_OnlyPicksBiomesAllowedAtTheDistance()
    {
        var inner = MakeBiome("inner", 50, 0, 1000);
        var outer = MakeBiome("outer", 50, 1000, 0);
        var biomes = new[] { inner, outer };

        for (int n = 0; n < 256; n++)
        {
            Assert.Same(inner, BiomeSelector.Select(biomes, (byte)n, 500, null));
            Assert.Same(outer, BiomeSelector.Select(biomes, (byte)n, 5000, null));
        }
    }

    [Fact]
    public void BiomeSelector_RenormalizesChancesAndFallsBack()
    {
        var a = MakeBiome("a", 50);
        var b = MakeBiome("b", 50);
        var far = MakeBiome("far", 50, 10000, 0);
        var fallback = MakeBiome("fallback", 1);

        Assert.Same(a, BiomeSelector.Select(new[] { a, far, b }, 0, 10, fallback));
        Assert.Same(b, BiomeSelector.Select(new[] { a, far, b }, 255, 10, fallback));
        Assert.Same(fallback, BiomeSelector.Select(new[] { far }, 100, 10, fallback));
        Assert.Null(BiomeSelector.Select(new[] { far }, 100, 10, null));
    }

    [Fact]
    public void Planner_IsDeterministicAndUsesLocalPositions()
    {
        var biome = MakeBiome("field", 100);
        biome.AsteroidChances[MakeAsteroid("rock")] = 1;
        var generator = MakeGenerator(biome);

        var first = Plan(generator, biome);
        var second = Plan(generator, biome);

        Assert.NotEmpty(first);
        Assert.Equal(first.Select(a => (a.Id, a.radiusBlocks, a.positionWorld)), second.Select(a => (a.Id, a.radiusBlocks, a.positionWorld)));
        foreach (var asteroid in first)
        {
            Assert.InRange(asteroid.positionInSector.X, 0f, Sector.SizeBlocks);
            Assert.Equal(asteroid.positionInSector + new Vector3(8192, 0, 0), asteroid.positionWorld);
            Assert.Equal(asteroid.positionWorld.Length, asteroid.generationDistance, 3);
            Assert.Equal(GeneratorVersions.Rules, asteroid.generatorVersion);
        }
    }

    [Fact]
    public void Planner_KeepsAsteroidsByBiomeDensity()
    {
        var full = MakeBiome("full", 100, density: 100);
        var half = MakeBiome("half", 100, density: 50);
        var none = MakeBiome("none", 100, density: 0);
        foreach (var biome in new[] { full, half, none }) biome.AsteroidChances[MakeAsteroid("rock")] = 1;
        var generator = MakeGenerator(full, half, none);

        int fullCount = Plan(generator, full).Count;
        int halfCount = Plan(generator, half).Count;

        Assert.Empty(Plan(generator, none));
        Assert.InRange(halfCount, fullCount / 4, fullCount * 3 / 4);
    }

    [Fact]
    public void Planner_RespectsTheBiomeSpacing()
    {
        var sparse = MakeBiome("sparse", 100, spacing: 1500);
        sparse.AsteroidChances[MakeAsteroid("rock")] = 1;

        var planned = Plan(MakeGenerator(sparse), sparse);

        Assert.NotEmpty(planned);
        for (int i = 0; i < planned.Count; i++)
        for (int j = i + 1; j < planned.Count; j++)
            Assert.True((planned[i].positionInSector - planned[j].positionInSector).Length >= 1500);
    }

    [Fact]
    public void Planner_SkipsAsteroidTypesOutsideTheirDistance()
    {
        var biome = MakeBiome("field", 100);
        var near = MakeAsteroid("near");
        var never = MakeAsteroid("never", minDistance: 1_000_000);
        biome.AsteroidChances[near] = 1;
        biome.AsteroidChances[never] = 100;

        var planned = Plan(MakeGenerator(biome), biome);

        Assert.NotEmpty(planned);
        Assert.All(planned, a => Assert.Same(near, a.asteroid));
    }

    [Fact]
    public void Planner_PicksEveryAsteroidIndependently()
    {
        var rock = MakeAsteroid("rock", 10, 20);
        var ice = MakeAsteroid("ice", 10, 20);
        var iceBig = MakeAsteroid("ice", 30, 60);
        var biomeA = MakeBiome("a", 100);
        biomeA.AsteroidChances[rock] = 50;
        biomeA.AsteroidChances[ice] = 50;
        var biomeB = MakeBiome("b", 100);
        biomeB.AsteroidChances[rock] = 50;
        biomeB.AsteroidChances[iceBig] = 50;

        var rocksA = Plan(MakeGenerator(biomeA), biomeA).Where(a => a.asteroid == rock).Select(a => (a.Id, a.radiusBlocks));
        var rocksB = Plan(MakeGenerator(biomeB), biomeB).Where(a => a.asteroid == rock).Select(a => (a.Id, a.radiusBlocks));

        Assert.Equal(rocksA, rocksB);
    }

    [Fact]
    public void Planner_AppliesTheRadiusMultiplier()
    {
        var biome = new Biome(new BiomeJSON { Id = "big", SpawnChance = 100, RadiusMultiplier = 2f });
        biome.AsteroidChances[MakeAsteroid("rock", 10, 10)] = 1;

        Assert.All(Plan(MakeGenerator(biome), biome), a => Assert.Equal(20, a.radiusBlocks));
    }

    [Fact]
    public void LegacyAsteroidPositions_StayTheSame()
    {
        var points = SpaceMath.Sector.GenerateAsteroidPositions(987654321L, new Vector3(8192, 0, -8192), 40, 60, 5, 128, true);

        Assert.Equal(LegacyPointsCount, points.Length);
        Assert.Equal(LegacyPointsHash, Hash(points));
    }

    private const int LegacyPointsCount = 53;
    private const long LegacyPointsHash = -945495975894315295;

    private static long Hash(IEnumerable<Vector3> points)
    {
        long hash = 17;
        foreach (var p in points) hash = hash * 31 + (long)(p.X * 7 + p.Y * 13 + p.Z * 17);
        return hash;
    }

    [Fact]
    public void VeinRules_CheckDepthDistanceAndBiomeMultipliers()
    {
        var vein = new AsteroidVein { SpawnChance = 1000, MinDepth = 0.3f, MaxDepth = 0.8f, Distance = new DistanceRange(100, 0) };
        vein.BiomeMultipliers["rich"] = 300;
        var rich = new Biome(new BiomeJSON { Id = "rich", OreMultiplier = 2f });

        Assert.False(VeinRules.Allows(vein, 0.1f, 500));
        Assert.False(VeinRules.Allows(vein, 0.5f, 50));
        Assert.True(VeinRules.Allows(vein, 0.5f, 500));
        Assert.Equal(1000, VeinRules.Chance(vein, null));
        Assert.Equal(6000, VeinRules.Chance(vein, rich));
    }

    [Fact]
    public void Cavities_AreDeterministicAndCarveInside()
    {
        var settings = new AsteroidCavitiesJSON { MinCount = 2, MaxCount = 2, MinRadius = 4, MaxRadius = 4 };
        var spheres = AsteroidCavities.Create(42, 30f, settings);

        Assert.Equal(2, spheres.Count);
        Assert.Equal(spheres, AsteroidCavities.Create(42, 30f, settings));
        Assert.All(spheres, s => Assert.True(s.Center.Length + s.Radius <= 30f * 0.7f + 0.001f));

        var padded = new int[Chunk.Size + 2, Chunk.Size + 2, Chunk.Size + 2];
        for (int x = 0; x < Chunk.Size + 2; x++)
        for (int y = 0; y < Chunk.Size + 2; y++)
        for (int z = 0; z < Chunk.Size + 2; z++)
            padded[x, y, z] = 5;

        var sphere = new List<(Vector3, float)> { (new Vector3(10, 10, 10), 3f) };
        AsteroidCavities.Carve(padded, new Vector3SByte(0, 0, 0), sphere);

        Assert.Equal(0, padded[11, 11, 11]);
        Assert.Equal(5, padded[16, 11, 11]);
    }

    [Fact]
    public void HollowCore_OnlyAppliesToRuleWorlds()
    {
        var asteroid = new AsteroidData("hollow", "Hollow") { DensityThreshold = 0, HollowCorePercent = 50, Layers = new[] { new AsteroidLayer { FillBlockID = 7, Ratio = 100 } } };
        var size = new Vector3(64);

        var legacy = new AsteroidVoxelDataGenerator(size, 1f, asteroid, 3).GeneratePaddedDataForChunk(new Vector3SByte(0, 0, 0));
        var rules = new AsteroidVoxelDataGenerator(size, 1f, asteroid, 3, new AsteroidGenerationContext(GeneratorVersions.Rules, null, 0f, 32)).GeneratePaddedDataForChunk(new Vector3SByte(0, 0, 0));

        Assert.Equal(7, legacy[1, 1, 1]);
        Assert.Equal(0, rules[1, 1, 1]);
    }

    [Fact]
    public void LayerThresholds_MatchTheRatios()
    {
        var layers = new[] { new AsteroidLayer { Ratio = 100 }, new AsteroidLayer { Ratio = 70 }, new AsteroidLayer { Ratio = 30 } };
        var thresholds = AsteroidLayers.Thresholds(layers);

        Assert.Equal(new[] { 0.5f, 0.85f, 1f }, thresholds.Select(t => MathF.Round(t, 4)));
        Assert.Equal(0, AsteroidLayers.IndexAt(thresholds, 0.2f));
        Assert.Equal(1, AsteroidLayers.IndexAt(thresholds, 0.6f));
        Assert.Equal(2, AsteroidLayers.IndexAt(thresholds, 1f));
        Assert.Equal(-1, AsteroidLayers.IndexAt(null, 0.5f));
        Assert.Equal(0.75f, AsteroidLayers.Depth(5f, 20f));
    }

    [Fact]
    public void WorldInfo_WithoutGeneratorVersion_IsLegacy()
    {
        var info = JsonSerializer.Deserialize<WorldInfo>("{ \"Name\": \"Old\" }")!;

        Assert.Equal(GeneratorVersions.Legacy, info.GeneratorVersion);
        Assert.False(GeneratorVersions.UsesRules(info.GeneratorVersion));
        Assert.Equal(GeneratorVersions.Legacy, GeneratorVersions.Normalize(0));
    }

    [Fact]
    public void GenerationTags_RoundTrip()
    {
        var root = new SharpNBT.CompoundTag("entity");
        var data = new NotGeneratedEntity { generatorVersion = GeneratorVersions.Rules, generationDistance = 1234.5f };
        Spacebox.Game.Generation.Tools.AsteroidGenerationTags.Write(root, data);

        var restored = new NotGeneratedEntity();
        Spacebox.Game.Generation.Tools.AsteroidGenerationTags.Read(root, restored);

        Assert.Equal(GeneratorVersions.Rules, restored.generatorVersion);
        Assert.Equal(1234.5f, restored.generationDistance);

        var legacy = new NotGeneratedEntity { generatorVersion = GeneratorVersions.Rules };
        Spacebox.Game.Generation.Tools.AsteroidGenerationTags.Read(new SharpNBT.CompoundTag("old"), legacy);
        Assert.Equal(GeneratorVersions.Legacy, legacy.generatorVersion);
    }
}
