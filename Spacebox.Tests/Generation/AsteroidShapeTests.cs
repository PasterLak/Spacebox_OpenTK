using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Shapes.Asteroids;
using Spacebox.Game.Generation.Structures;
using Xunit;

namespace Spacebox.Tests.Generation;

public class AsteroidShapeTests
{
    [Fact]
    public void Sdf_PrimitivesHaveTheRightSign()
    {
        Assert.Equal(-2f, ShapeSdf.Sphere(Vector3.Zero, Vector3.Zero, 2f), 4);
        Assert.Equal(1f, ShapeSdf.Sphere(new Vector3(3, 0, 0), Vector3.Zero, 2f), 4);
        Assert.True(ShapeSdf.Ellipsoid(new Vector3(0.9f, 0, 0), Vector3.Zero, new Vector3(1f, 0.5f, 0.5f)) < 0f);
        Assert.True(ShapeSdf.Ellipsoid(new Vector3(0, 0.6f, 0), Vector3.Zero, new Vector3(1f, 0.5f, 0.5f)) > 0f);
        Assert.True(ShapeSdf.Torus(Vector3.Zero, 0.6f, 0.2f) > 0f);
        Assert.Equal(-0.2f, ShapeSdf.Torus(new Vector3(0.6f, 0, 0), 0.6f, 0.2f), 4);
        Assert.True(ShapeSdf.Capsule(new Vector3(0.5f, 0.05f, 0), Vector3.Zero, Vector3.UnitX, 0.1f) < 0f);
        Assert.True(ShapeSdf.Cone(new Vector3(1, 0.1f, 0), Vector3.UnitX, 0.5f) < 0f);
        Assert.True(ShapeSdf.Cone(new Vector3(-1, 0, 0), Vector3.UnitX, 0.5f) > 0f);
        Assert.True(ShapeSdf.SmoothUnion(0.5f, 0.6f, 0.2f) <= 0.5f);
        Assert.True(ShapeSdf.SmoothSubtract(-1f, -1f, 0.1f) > 0f);
    }

    [Fact]
    public void Shape_IsDeterministicPerSeed()
    {
        var settings = new AsteroidShapeJSON { Lobes = new AsteroidLobesJSON { Chance = 100 }, Craters = new AsteroidCratersJSON { MinCount = 2, MaxCount = 4 } };
        var a = new AsteroidShape(settings, 77);
        var b = new AsteroidShape(settings, 77);
        var c = new AsteroidShape(settings, 78);

        bool differs = false;
        foreach (var point in SamplePoints(30f))
        {
            Assert.Equal(a.Distance(point, 30f), b.Distance(point, 30f));
            differs |= MathF.Abs(a.Distance(point, 30f) - c.Distance(point, 30f)) > 1e-4f;
        }
        Assert.True(differs);
    }

    [Fact]
    public void Shape_NeverReachesBeyondTheRadius()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var shape = new AsteroidShape(new AsteroidShapeJSON { Warp = 0.6f, Lobes = new AsteroidLobesJSON { Chance = 100, MaxCount = 4, MaxSize = 1f } }, seed);
            foreach (var point in SamplePoints(30f))
            {
                if (point.Length > 30f * 0.98f) Assert.True(shape.Distance(point, 30f) > 0f);
            }
        }
    }

    [Fact]
    public void Torus_HasAnEmptyCenterAndASolidRing()
    {
        var shape = new AsteroidShape(new AsteroidShapeJSON { Warp = 0f }, 5, AsteroidShapeKind.Torus);

        Assert.Equal(AsteroidShapeKind.Torus, shape.Kind);
        Assert.True(shape.Distance(Vector3.Zero, 30f) > 0f);
        Assert.Contains(SamplePoints(30f), p => shape.Distance(p, 30f) < 0f);
    }

    [Fact]
    public void Chances_PickTheForms()
    {
        var always = new AsteroidShapeJSON { Torus = new AsteroidTorusJSON { Chance = 100 }, Bite = new AsteroidBiteJSON { Chance = 100 } };
        var shape = new AsteroidShape(always, 9);

        Assert.Equal(AsteroidShapeKind.Torus, shape.Kind);
        Assert.True(shape.HasBite);
        Assert.Equal(AsteroidShapeKind.Double, new AsteroidShape(new AsteroidShapeJSON { Double = new AsteroidDoubleJSON { Chance = 100 } }, 9).Kind);
        Assert.Equal(AsteroidShapeKind.Blob, new AsteroidShape(new AsteroidShapeJSON(), 9).Kind);
    }

    [Fact]
    public void Depth_IsZeroAtTheSurfaceAndGrowsInside()
    {
        var shape = new AsteroidShape(new AsteroidShapeJSON { Warp = 0f, Stretch = 0f, Lobes = new AsteroidLobesJSON { Chance = 0 }, Craters = new AsteroidCratersJSON { MaxCount = 0 } }, 3);

        Assert.True(shape.Depth(Vector3.Zero, 30f) > 0.9f);
        Assert.Equal(0f, shape.Depth(new Vector3(29f, 0, 0), 30f));
    }

    [Fact]
    public void SolidMask_KeepsOnlyTheLargestPiece()
    {
        var mask = new AsteroidSolidMask(-8, 16);
        for (int x = -4; x <= 0; x++)
        for (int y = -4; y <= 0; y++)
        for (int z = -4; z <= 0; z++)
            mask.Set(x, y, z, true);
        mask.Set(5, 5, 5, true);
        for (int x = 3; x <= 4; x++)
        for (int y = -7; y <= -6; y++)
        for (int z = 3; z <= 4; z++)
            mask.Set(x, y, z, true);

        int removed = mask.KeepLargestComponent();

        Assert.Equal(9, removed);
        Assert.True(mask.IsSolid(-2, -2, -2));
        Assert.False(mask.IsSolid(5, 5, 5));
        Assert.False(mask.IsSolid(3, -7, 3));
        Assert.False(mask.IsSolid(100, 0, 0));
    }

    [Theory]
    [InlineData(AsteroidShapeKind.Blob, 11)]
    [InlineData(AsteroidShapeKind.Double, 12)]
    [InlineData(AsteroidShapeKind.Torus, 13)]
    public void ShapedAsteroid_HasNoFloatingBlocks(AsteroidShapeKind kind, int seed)
    {
        var settings = new AsteroidShapeJSON
        {
            Roughness = 0.45f,
            Warp = 0.35f,
            Lobes = new AsteroidLobesJSON { Chance = 100 },
            Double = new AsteroidDoubleJSON { Chance = kind == AsteroidShapeKind.Double ? (byte)100 : (byte)0 },
            Torus = new AsteroidTorusJSON { Chance = kind == AsteroidShapeKind.Torus ? (byte)100 : (byte)0 },
            Bite = new AsteroidBiteJSON { Chance = 100 },
            Craters = new AsteroidCratersJSON { MinCount = 3, MaxCount = 5 }
        };
        var asteroid = new AsteroidData("shaped", "Shaped")
        {
            DensityThreshold = 35,
            NoiseOctaves = 3,
            NoiseScale = 1f,
            UsePerlinWorms = true,
            WormSettings = new WormSettingsJSON { MinCount = 2, MaxCount = 4, DiameterInBlocks = 3, MaxTunnelLength = 40 },
            Shape = settings,
            Layers = new[] { new AsteroidLayer { FillBlockID = 1, Ratio = 100 } }
        };

        const int radius = 24;
        var shape = AsteroidGenerationContext.CreateShape(GeneratorVersions.Shapes, asteroid, seed);
        var context = new AsteroidGenerationContext(GeneratorVersions.Shapes, null, 0f, radius, shape);
        var generator = new AsteroidVoxelDataGenerator(new Vector3(radius * 2), 1f, asteroid, seed, context);
        var mask = AsteroidSolidMaskBuilder.TryBuild(generator, asteroid, seed, context, radius * 2);
        Assert.NotNull(mask);
        Assert.Equal(kind, shape.Kind);

        var solid = new HashSet<Vector3i>();
        AsteroidChunkBuilder.GetChunkRange(radius * 2, out int min, out int max);
        for (int cx = min; cx <= max; cx++)
        for (int cy = min; cy <= max; cy++)
        for (int cz = min; cz <= max; cz++)
        {
            var data = generator.GeneratePaddedDataForChunk(new Vector3SByte(cx, cy, cz), mask);
            for (int x = 1; x <= Chunk.Size; x++)
            for (int y = 1; y <= Chunk.Size; y++)
            for (int z = 1; z <= Chunk.Size; z++)
                if (data[x, y, z] != 0) solid.Add(new Vector3i(cx * Chunk.Size + x - 1, cy * Chunk.Size + y - 1, cz * Chunk.Size + z - 1));
        }

        Assert.True(solid.Count > 300);
        Assert.Equal(solid.Count, ConnectedCount(solid));
    }

    [Fact]
    public void OlderVersions_IgnoreTheShape()
    {
        var asteroid = new AsteroidData("shaped", "Shaped") { Shape = new AsteroidShapeJSON() };

        Assert.Null(AsteroidGenerationContext.CreateShape(GeneratorVersions.Rules, asteroid, 1));
        Assert.Null(new AsteroidGenerationContext(GeneratorVersions.Rules, null, 0f, 20, new AsteroidShape(asteroid.Shape, 1)).Shape);
        Assert.NotNull(AsteroidGenerationContext.CreateShape(GeneratorVersions.Shapes, asteroid, 1));
        Assert.Null(AsteroidGenerationContext.CreateShape(GeneratorVersions.Shapes, new AsteroidData("plain", "Plain"), 1));
    }

    private static int ConnectedCount(HashSet<Vector3i> cells)
    {
        var start = cells.First();
        var seen = new HashSet<Vector3i> { start };
        var queue = new Queue<Vector3i>();
        queue.Enqueue(start);
        var offsets = new[] { Vector3i.UnitX, -Vector3i.UnitX, Vector3i.UnitY, -Vector3i.UnitY, Vector3i.UnitZ, -Vector3i.UnitZ };
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var offset in offsets)
            {
                var next = cell + offset;
                if (cells.Contains(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return seen.Count;
    }

    private static IEnumerable<Vector3> SamplePoints(float radius)
    {
        const int steps = 12;
        for (int x = 0; x < steps; x++)
        for (int y = 0; y < steps; y++)
        for (int z = 0; z < steps; z++)
            yield return (new Vector3(x, y, z) / (steps - 1) * 2f - Vector3.One) * radius;
    }

    [Fact]
    public void ShapeChance_KeepsSomeAsteroidsClassic()
    {
        var asteroid = new AsteroidData("mixed", "Mixed") { Shape = new AsteroidShapeJSON { Chance = 70 } };
        int shaped = 0;
        for (int seed = 0; seed < 1000; seed++)
        {
            var first = AsteroidGenerationContext.CreateShape(GeneratorVersions.Shapes, asteroid, seed);
            Assert.Equal(first != null, AsteroidGenerationContext.CreateShape(GeneratorVersions.Shapes, asteroid, seed) != null);
            if (first != null) shaped++;
        }

        Assert.InRange(shaped, 640, 760);
        Assert.Null(AsteroidGenerationContext.CreateShape(GeneratorVersions.Shapes, new AsteroidData("never", "Never") { Shape = new AsteroidShapeJSON { Chance = 0 } }, 1));
    }

    [Fact]
    public void Planner_GivesAsteroidsTheWorldVersion()
    {
        var biome = new Biome(new BiomeJSON { Id = "field", SpawnChance = 100 });
        biome.AsteroidChances[new AsteroidData("rock", "Rock") { MinRadius = 10, MaxRadius = 20 }] = 1;
        var generator = new WorldGenerator { MinAsteroidsInSector = 20, MaxAsteroidsInSector = 30, RejectionSamples = 5, MinDistanceBetweenAsteroids = 128, Biomes = new[] { biome } };

        var planned = SectorAsteroidPlanner.Plan(generator, 5, Vector3.Zero, _ => biome, p => (long)p.LengthSquared, GeneratorVersions.Shapes);

        Assert.NotEmpty(planned);
        Assert.All(planned, a => Assert.Equal(GeneratorVersions.Shapes, a.generatorVersion));
    }
}
