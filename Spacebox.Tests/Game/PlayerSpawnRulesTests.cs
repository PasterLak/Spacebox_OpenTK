using System.Text.Json;
using Spacebox.Game.Generation.Structures;
using Xunit;

namespace Spacebox.Tests.Game;

public class PlayerSpawnRulesTests
{
    private static readonly PlayerSpawnRules Rules = new PlayerSpawnRules(24, new[] { "asteroid_icy" });

    [Fact]
    public void PickIndex_OnlyPicksAllowedAsteroids()
    {
        var candidates = new List<(string, int)> { ("light", 14), ("asteroid_icy", 30), ("medium", 28), ("heavy", 50), ("light", 12) };

        for (int seed = 0; seed < 200; seed++)
        {
            int index = Rules.PickIndex(candidates, new Random(seed));
            Assert.Contains(index, new[] { 2, 3 });
        }
    }

    [Fact]
    public void PickIndex_FallsBackToTheLargestNotExcluded()
    {
        var candidates = new List<(string, int)> { ("light", 14), ("asteroid_icy", 40), ("light", 16) };

        Assert.Equal(2, Rules.PickIndex(candidates, new Random(1)));
    }

    [Fact]
    public void PickIndex_FallsBackToTheLargestWhenEverythingIsExcluded()
    {
        var candidates = new List<(string, int)> { ("asteroid_icy", 20), ("asteroid_icy", 35) };

        Assert.Equal(1, Rules.PickIndex(candidates, new Random(1)));
        Assert.Equal(-1, Rules.PickIndex(new List<(string, int)>(), new Random(1)));
    }

    [Fact]
    public void DefaultRules_AllowEverything()
    {
        Assert.True(PlayerSpawnRules.Default.IsAllowed("asteroid_icy", 1));
        Assert.True(PlayerSpawnRules.FromJson(null).IsAllowed("x", 0));
        Assert.True(PlayerSpawnRules.FromJson(new PlayerSpawnJSON()).IsAllowed("light", 12));
    }

    [Fact]
    public void DefaultGameSet_SpawnsOnlyNearLargeRockyAsteroids()
    {
        string folder = FindGeneration();
        var parameters = Spacebox.Game.Player.Respawn.SpawnParameters.TryLoad(Path.GetDirectoryName(folder));
        Assert.NotNull(parameters);
        var rules = PlayerSpawnRules.FromJson(parameters.SpawnSettings);

        foreach (var file in Directory.GetFiles(Path.Combine(folder, "Asteroids"), "*.json"))
        {
            var asteroid = JsonSerializer.Deserialize<AsteroidJSON>(File.ReadAllText(file));
            bool icy = asteroid.Id.Contains("icy");
            bool small = asteroid.MaxRadius < 20;

            if (icy || small) Assert.False(rules.IsAllowed(asteroid.Id, asteroid.MaxRadius), $"{asteroid.Id} must not be a spawn asteroid");
        }

        Assert.True(rules.IsAllowed("medium", 24));
    }

    private static string FindGeneration()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", "Mods", "GameSets", "Default", "Generation");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Generation");
    }

    [Fact]
    public void Candidates_PreferTheBiomeAndKeepTheDistance()
    {
        var rules = new PlayerSpawnRules(0, null) { PreferredBiome = "dense", Distance = new DistanceRange(1000, 5000) };
        var candidates = new List<SpawnCandidate>
        {
            new SpawnCandidate("rock", 30, "field", 2000),
            new SpawnCandidate("rock", 30, "dense", 9000),
            new SpawnCandidate("rock", 30, "dense", 3000),
            new SpawnCandidate("rock", 30, "dense", 500)
        };

        for (int seed = 0; seed < 50; seed++) Assert.Equal(2, rules.PickIndex(candidates, new Random(seed)));

        var noBiome = new List<SpawnCandidate> { new SpawnCandidate("rock", 30, "field", 2000), new SpawnCandidate("rock", 30, "field", 9000) };
        Assert.Equal(0, rules.PickIndex(noBiome, new Random(1)));

        var outside = new List<SpawnCandidate> { new SpawnCandidate("rock", 30, "field", 90000) };
        Assert.Equal(0, rules.PickIndex(outside, new Random(1)));
    }

    [Fact]
    public void SpawnStats_ArePercentOfTheMaximum()
    {
        var rules = PlayerSpawnRules.FromJson(new PlayerSpawnJSON { HealthOnSpawn = 50, PowerOnSpawn = 0, OxygenOnSpawn = 150 });

        Assert.Equal(50, PlayerSpawnRules.ValueOf(100, rules.HealthPercent));
        Assert.Equal(0, PlayerSpawnRules.ValueOf(80, rules.PowerPercent));
        Assert.Equal(120, PlayerSpawnRules.ValueOf(120, rules.OxygenPercent));
        Assert.Equal(1, PlayerSpawnRules.FromJson(new PlayerSpawnJSON { HealthOnSpawn = 0 }).HealthPercent);
        Assert.Equal("", PlayerSpawnRules.FromJson(new PlayerSpawnJSON { PreferredSpawnBiome = "none" }).PreferredBiome);
    }
}
