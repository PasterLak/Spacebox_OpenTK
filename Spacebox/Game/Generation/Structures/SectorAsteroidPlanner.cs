using Engine.Generation;
using OpenTK.Mathematics;
using Engine.Utils;

namespace Spacebox.Game.Generation.Structures;

public static class SectorAsteroidPlanner
{
    public static List<NotGeneratedEntity> Plan(WorldGenerator generator, long sectorSeed, Vector3 sectorOrigin, Func<Vector3, Biome> biomeAt, Func<Vector3, long> idFor)
    {
        var result = new List<NotGeneratedEntity>();
        int maxDensity = MaxDensity(generator);
        if (maxDensity == 0) return result;

        int intSeed = SeedHelper.ToIntSeed(sectorSeed);
        var random = new Random(intSeed);
        int baseCount = random.Next(generator.MinAsteroidsInSector, generator.MaxAsteroidsInSector);

        var settings = new GeneratorSettings
        {
            Count = (int)Math.Min(int.MaxValue, (long)baseCount * maxDensity / 100),
            RejectionSamples = generator.RejectionSamples,
            Seed = intSeed,
            Round = true
        };

        var points = new SimplePoissonDiscGenerator(generator.MinDistanceBetweenAsteroids).GenerateLocal(in settings);
        foreach (var local in points)
        {
            var world = sectorOrigin + local;
            long id = idFor(world);

            var biome = biomeAt(local);
            if (biome == null || biome.AsteroidChances.Count == 0) continue;

            var pointRandom = new Random(SeedHelper.ToIntSeed(id));
            if (pointRandom.Next(maxDensity) >= biome.Density) continue;
            if (IsTooClose(result, local, biome.MinDistanceBetweenAsteroids, generator.MinDistanceBetweenAsteroids)) continue;

            float distance = world.Length;
            var asteroid = SelectAsteroid(biome, distance, pointRandom);
            if (asteroid == null) continue;

            int radius = pointRandom.Next(asteroid.MinRadius, asteroid.MaxRadius + 1);
            result.Add(new NotGeneratedEntity
            {
                Id = id,
                asteroid = asteroid,
                biome = biome,
                radiusBlocks = AsteroidRadius.Clamp((int)MathF.Round(radius * biome.RadiusMultiplier)),
                positionInSector = local,
                positionWorld = world,
                rotation = Vector3.Zero,
                FileName = id.ToString(),
                generatorVersion = GeneratorVersions.Rules,
                generationDistance = distance
            });
        }

        return result;
    }

    public static int MaxDensity(WorldGenerator generator)
    {
        int max = generator.FallbackBiome?.Density ?? 0;
        foreach (var biome in generator.Biomes)
        {
            if (biome != null && biome.Density > max) max = biome.Density;
        }
        return max;
    }

    public static AsteroidData SelectAsteroid(Biome biome, float distanceFromCenter, Random random)
    {
        int total = 0;
        foreach (var pair in biome.AsteroidChances)
        {
            if (pair.Key.Distance.Contains(distanceFromCenter)) total += pair.Value;
        }

        if (total == 0) return null;

        int value = random.Next(total);
        int sum = 0;
        foreach (var pair in biome.AsteroidChances)
        {
            if (!pair.Key.Distance.Contains(distanceFromCenter)) continue;

            sum += pair.Value;
            if (value < sum) return pair.Key;
        }

        return null;
    }

    private static bool IsTooClose(List<NotGeneratedEntity> accepted, Vector3 local, int biomeSpacing, int globalSpacing)
    {
        if (biomeSpacing <= globalSpacing) return false;

        float min2 = (float)biomeSpacing * biomeSpacing;
        for (int i = 0; i < accepted.Count; i++)
        {
            if ((accepted[i].positionInSector - local).LengthSquared < min2) return true;
        }
        return false;
    }
}
