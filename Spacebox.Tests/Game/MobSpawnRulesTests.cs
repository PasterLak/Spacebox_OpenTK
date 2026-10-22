using OpenTK.Mathematics;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game;
using Spacebox.Game.Mobs;
using Spacebox.Game.Mobs.Spawning;
using Spacebox.Game.Resource;
using Xunit;

namespace Spacebox.Tests.Game;

public class MobSpawnRulesTests
{
    private static AsteroidData Asteroid(params AsteroidMobRule[] rules)
    {
        return new AsteroidData("rock", "Rock")
        {
            Mobs = rules,
            Layers = new[] { new AsteroidLayer { Ratio = 50 }, new AsteroidLayer { Ratio = 50 } }
        };
    }

    [Fact]
    public void OnlyMobsListedByAnAsteroidUseRules()
    {
        var rules = new MobSpawnRules(new[] { Asteroid(new AsteroidMobRule { MobId = "parasite" }), Asteroid() });

        Assert.True(rules.IsListed("Parasite"));
        Assert.False(rules.IsListed("crab"));
        Assert.False(new MobSpawnRules(null).IsListed("parasite"));
    }

    [Fact]
    public void FindRule_ChecksMobBiomeAndDistance()
    {
        var biomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "dense" };
        var rule = new AsteroidMobRule { MobId = "parasite", Biomes = biomes, Distance = new DistanceRange(1000, 0) };
        var asteroid = Asteroid(rule);

        Assert.Same(rule, MobSpawnRules.FindRule(asteroid, "parasite", "dense", 2000));
        Assert.Null(MobSpawnRules.FindRule(asteroid, "parasite", "field", 2000));
        Assert.Null(MobSpawnRules.FindRule(asteroid, "parasite", "dense", 500));
        Assert.Null(MobSpawnRules.FindRule(asteroid, "crab", "dense", 2000));
        Assert.Null(MobSpawnRules.FindRule(null, "parasite", "dense", 2000));
    }

    [Fact]
    public void Limits_FallBackToTheMobSpawnSection()
    {
        var defaults = new MobSpawnJSON { MaxPerChunk = 2, MaxLightLevel = 0.5f };

        var legacy = MobSpawnRules.Limits(defaults, null);
        Assert.Equal(2, legacy.MaxPerChunk);
        Assert.Equal(0, legacy.MaxPerEntity);
        Assert.Equal(0.5f, legacy.MaxLightLevel);

        var limits = MobSpawnRules.Limits(defaults, new AsteroidMobRule { MaxPerChunk = 5, MaxPerEntity = 3, Chance = 40 });
        Assert.Equal(5, limits.MaxPerChunk);
        Assert.Equal(3, limits.MaxPerEntity);
        Assert.Equal(0.5f, limits.MaxLightLevel);
        Assert.Equal(40, limits.Chance);
    }

    [Fact]
    public void AllowsCell_ChecksLayerAndDepth()
    {
        var inner = new AsteroidMobRule { MobId = "parasite", Layers = new[] { 1 } };
        var deep = new AsteroidMobRule { MobId = "parasite", MinDepth = 0.8f };
        var asteroid = Asteroid(inner, deep);

        Assert.False(MobSpawnRules.AllowsCell(inner, asteroid, AsteroidLayers.Depth(18f, 20f)));
        Assert.True(MobSpawnRules.AllowsCell(inner, asteroid, AsteroidLayers.Depth(2f, 20f)));
        Assert.False(MobSpawnRules.AllowsCell(deep, asteroid, AsteroidLayers.Depth(10f, 20f)));
        Assert.True(MobSpawnRules.AllowsCell(deep, asteroid, AsteroidLayers.Depth(1f, 20f)));
        Assert.True(MobSpawnRules.AllowsCell(null, asteroid, AsteroidLayers.Depth(19f, 20f)));
    }

    [Fact]
    public void Loader_ReadsOldAsteroidJsonWithDefaultsAndDropsUnknownMobs()
    {
        var asteroid = GenerationLoader.ConvertAsteroid(new AsteroidJSON { Id = "old", MinRadius = 10, MaxRadius = 12, Mobs = new[] { new AsteroidMobJSON { Mob = "no_such_mob_for_tests" } } });

        Assert.True(asteroid.Distance.IsUnlimited);
        Assert.Equal(0, asteroid.HollowCorePercent);
        Assert.Null(asteroid.Cavities);
        Assert.Empty(asteroid.Mobs);
    }

    [Fact]
    public void Loader_ResolvesKnownMobsByShortId()
    {
        const string id = "test_mob_for_spawn_rules";
        GameAssets.Mobs[id] = new MobDefinition { Id = id };
        try
        {
            var asteroid = GenerationLoader.ConvertAsteroid(new AsteroidJSON
            {
                Id = "rock",
                Mobs = new[] { new AsteroidMobJSON { Mob = "default:" + id, MaxPerEntity = 2, MinDepth = 0.9f, MaxDepth = 0.1f } }
            });

            var rule = Assert.Single(asteroid.Mobs);
            Assert.Equal(id, rule.MobId);
            Assert.Equal(2, rule.MaxPerEntity);
            Assert.Equal(0.1f, rule.MinDepth);
            Assert.Equal(0.9f, rule.MaxDepth);
        }
        finally
        {
            GameAssets.Mobs.Remove(id);
        }
    }
}
