using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;
using Spacebox.Generation;
using Xunit;

namespace Spacebox.Tests.Generation;

public class LegacyGenerationTests
{
    private const long HashBeforeGenerationRules = -1538472446419842866;

    private static AsteroidData GoldenAsteroid()
    {
        var asteroid = new AsteroidData("golden", "Golden") { DensityThreshold = 35, NoiseOctaves = 3, NoiseScale = 1f, HasOreDeposits = true };
        asteroid.Layers = new[]
        {
            new AsteroidLayer { FillBlockID = 2, Ratio = 100, Veins = new[] { new AsteroidVein { BlockId = 9, SpawnChance = 25000, MinVeinSize = 4, MaxVeinSize = 10 } } },
            new AsteroidLayer { FillBlockID = 3, Ratio = 70, Veins = new[] { new AsteroidVein { BlockId = 10, SpawnChance = 13000, MinVeinSize = 2, MaxVeinSize = 6, CanSpawnNearVoid = false } } },
            new AsteroidLayer { FillBlockID = 4, Ratio = 50 }
        };
        return asteroid;
    }

    private static long GoldenHash()
    {
        var asteroid = GoldenAsteroid();
        var generator = new AsteroidVoxelDataGenerator(new OpenTK.Mathematics.Vector3(64), 1f, asteroid, 777);
        long hash = 17;
        for (sbyte x = -1; x <= 0; x++)
        for (sbyte y = -1; y <= 0; y++)
        {
            var index = new Vector3SByte((int)x, (int)y, 0);
            var data = generator.GeneratePaddedDataForChunk(index);
            new AsteroidOreGenerator(asteroid, 1000 + x * 10 + y).ApplyOres(ref data);
            foreach (var v in data) hash = hash * 31 + v;
        }
        return hash;
    }

    [Fact]
    public void LegacyAsteroidChunks_StayBitIdentical()
    {
        Assert.Equal(HashBeforeGenerationRules, GoldenHash());
    }
}
