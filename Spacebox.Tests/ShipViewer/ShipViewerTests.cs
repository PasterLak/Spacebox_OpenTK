using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.ShipViewer;
using Spacebox.Game.Versioning;
using Spacebox.Game.Versioning.Steps;
using Spacebox.Tests.Versioning;
using Xunit;

namespace Spacebox.Tests.ShipViewer;

public class ShipViewerTests : IDisposable
{
    private const string Current = "0.1.5";
    private static readonly string[] StonePattern = { "default:air", "default:stone" };

    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_shipviewer_" + Guid.NewGuid().ToString("N"));
    private readonly WorldUpgradeRegistry _registry = WorldUpgradeRegistry.CreateDefault();

    private string Worlds => Path.Combine(_root, "Worlds");
    private string Scratch => Path.Combine(_root, "Scratch");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string Build(string folder, Action<WorldFixtureBuilder> setup)
    {
        var builder = new WorldFixtureBuilder(Path.Combine(Worlds, folder)).Named(folder).Version(Current);
        setup?.Invoke(builder);
        return builder.Build();
    }

    private SavedWorld ReadWorld(string folder)
    {
        return SavedWorldCatalog.Read(folder, _registry, Current);
    }

    [Fact]
    public void Catalog_ClassifiesWorldsByVersionWithoutWritingFiles()
    {
        Build("Current", null);
        Build("Old", b => b.Version("0.1.2"));
        Build("TooOld", b => b.Version("0.1.0"));
        Build("Newer", b => b.Version("9.9.9"));
        Directory.CreateDirectory(Path.Combine(Worlds, "Broken"));
        File.WriteAllText(Path.Combine(Worlds, "Broken", WorldFileLayout.WorldFile), "{ not json");
        Directory.CreateDirectory(Path.Combine(Worlds, "NoWorldFile"));
        Build(WorldUpgrader.BackupFolderName, null);
        var before = WorldInspector.Snapshot(Worlds);

        var worlds = SavedWorldCatalog.Scan(Worlds, _registry, Current);

        Assert.Equal(new[] { "Broken", "Current", "Newer", "Old", "TooOld" }, worlds.Select(w => w.Name).ToArray());
        Assert.Equal(SavedWorldStatus.Corrupt, worlds[0].Status);
        Assert.Equal(SavedWorldStatus.Ready, worlds[1].Status);
        Assert.Equal(SavedWorldStatus.Unsupported, worlds[2].Status);
        Assert.Equal(SavedWorldStatus.NeedsUpgrade, worlds[3].Status);
        Assert.Equal(SavedWorldStatus.Unsupported, worlds[4].Status);
        Assert.True(worlds[3].CanBeViewed);
        Assert.False(worlds[4].CanBeViewed);
        Assert.Equal(before.Keys.OrderBy(k => k), WorldInspector.Snapshot(Worlds).Keys.OrderBy(k => k));
    }

    [Fact]
    public void Catalog_MissingDirectory_IsEmpty()
    {
        Assert.Empty(SavedWorldCatalog.Scan(Path.Combine(_root, "missing"), _registry, Current));
    }

    [Fact]
    public void Scanner_FindsPlayerBuiltEntitiesWithStatsAndSkipsAsteroids()
    {
        string folder = Build("Ships", b => b
            .Chunk(StonePattern, entity: "small")
            .Chunk(StonePattern, entity: "big").Chunk(StonePattern, entity: "big")
            .Chunk(StonePattern, entity: "asteroid").EntityId("asteroid", 42)
            .Chunk(new[] { "default:air" }, entity: "empty"));

        var result = ShipScanner.Scan(folder);

        Assert.Equal(0, result.UnreadableFiles);
        Assert.Equal(new[] { "big", "small" }, result.Ships.Select(s => s.Name).ToArray());

        var big = result.Ships[0].Stats;
        Assert.Equal(2, big.Chunks);
        Assert.Equal(32 * 32 * 32, big.Blocks);
        Assert.Equal(new Vector3i(0, 0, 1), big.Min);
        Assert.Equal(new Vector3i(64, 32, 32), big.Max);
        Assert.Equal(new Vector3i(64, 32, 31), big.Size);

        var small = result.Ships[1].Stats;
        Assert.Equal(1, small.Chunks);
        Assert.Equal(32 * 32 * 16, small.Blocks);
    }

    [Fact]
    public void Scanner_EntitiesWithoutShipPartsAreBases()
    {
        var parts = ShipPartBlocks.FromContent(DefaultGameSetContent.Content, "default");
        string folder = Build("Bases", b => b
            .Chunk(new[] { "default:stone", "default:thruster_weak" }, entity: "flyer")
            .Chunk(StonePattern, entity: "station"));

        var result = ShipScanner.Scan(folder, parts.Contains);

        Assert.True(result.Ships.Single(s => s.Name == "flyer").HasShipParts);
        Assert.False(result.Ships.Single(s => s.Name == "station").HasShipParts);
    }

    [Fact]
    public void ShipParts_ComeFromBlockTypes()
    {
        var parts = ShipPartBlocks.FromContent(DefaultGameSetContent.Content, "default");

        Assert.True(parts.Contains("default:thruster_weak"));
        Assert.False(parts.Contains("default:stone"));
    }

    [Fact]
    public void Labels_NumberShipsAndBasesSeparatelyAndKeepCustomNames()
    {
        var stats = new ShipBlockStats(1, 1, Vector3i.Zero, Vector3i.One);
        var result = new ShipScanResult(new List<SavedShip>
        {
            new SavedShip("a", "11", 11, stats, true),
            new SavedShip("b", "12", 12, stats, false),
            new SavedShip("c", "Falcon", 13, stats, true),
            new SavedShip("d", "14", 14, stats, true),
            new SavedShip("e", "15", 15, stats, false)
        }, 0);

        Assert.Equal(new[] { "Ship 1", "Base 1", "Falcon", "Ship 2", "Base 2" }, Enumerable.Range(0, 5).Select(result.Label));
    }

    [Fact]
    public void Scanner_EmptyWorldHasNoShips()
    {
        string folder = Build("Empty", null);

        var result = ShipScanner.Scan(folder);

        Assert.Empty(result.Ships);
        Assert.Equal(0, result.UnreadableFiles);
    }

    [Fact]
    public void Scanner_CountsCorruptEntityFiles()
    {
        string folder = Build("Corrupt", b => b.Chunk(StonePattern, entity: "ship"));
        string sector = Path.Combine(folder, WorldFileLayout.SectorsFolder, WorldFixtureBuilder.SectorFolderName);
        File.WriteAllBytes(Path.Combine(sector, "broken.entity"), new byte[] { 1, 2, 3, 4 });

        var result = ShipScanner.Scan(folder);

        Assert.Single(result.Ships);
        Assert.Equal(1, result.UnreadableFiles);
    }

    [Fact]
    public void TagReader_TreatsAirAndVoidAsEmpty()
    {
        Assert.True(ShipTagReader.IsEmpty("default:air"));
        Assert.True(ShipTagReader.IsEmpty("default:void"));
        Assert.True(ShipTagReader.IsEmpty("mymod:air"));
        Assert.True(ShipTagReader.IsEmpty(""));
        Assert.False(ShipTagReader.IsEmpty("default:stone"));
        Assert.Equal(new Vector3i(1, 2, 3), ShipTagReader.CellOf(1 * 1024 + 2 * 32 + 3));
    }

    [Fact]
    public void Loader_CurrentWorld_ReadsTheSavedFile()
    {
        string folder = Build("Current", b => b.Chunk(StonePattern, entity: "ship"));
        var world = ReadWorld(folder);
        var ship = ShipScanner.Scan(folder).Ships.Single();

        var tag = new ReadOnlyShipLoader(_registry, Current, Scratch).Load(world, ship);

        Assert.Equal(ship.Stats.Blocks, ShipTagReader.Measure(tag).Blocks);
    }

    [Fact]
    public void Loader_OldWorld_IsUpgradedInMemoryWithoutTouchingTheSave()
    {
        string folder = Build("Old", b => b
            .Version("0.1.2")
            .Chunk(new[] { "default:air", Upgrade_0_1_2_To_0_1_5.RemovedBasicEngine, "default:stone" }, entity: "ship"));
        var before = WorldInspector.Snapshot(Worlds);
        var world = ReadWorld(folder);
        var ship = ShipScanner.Scan(folder).Ships.Single();

        var tag = new ReadOnlyShipLoader(_registry, Current, Scratch).Load(world, ship);

        var palette = ChunkTagAccess.BlockPalette(ChunkTagAccess.Chunks(tag).Single());
        Assert.DoesNotContain(Upgrade_0_1_2_To_0_1_5.RemovedBasicEngine, palette);
        Assert.Contains("default:stone", palette);
        Assert.Equal(10922, ShipTagReader.Measure(tag).Blocks);

        var after = WorldInspector.Snapshot(Worlds);
        Assert.Equal(before.Keys.OrderBy(k => k), after.Keys.OrderBy(k => k));
        foreach (var pair in before) Assert.Equal(pair.Value, after[pair.Key]);
        Assert.Equal("0.1.2", WorldInspector.Version(folder));
        Assert.Empty(Directory.Exists(Scratch) ? Directory.GetFileSystemEntries(Scratch) : Array.Empty<string>());
    }

    [Fact]
    public void Loader_UnsupportedWorld_Throws()
    {
        string folder = Build("TooOld", b => b.Version("0.1.0").Chunk(StonePattern, entity: "ship"));
        var world = ReadWorld(folder);
        var ship = ShipScanner.Scan(folder).Ships.Single();

        Assert.Throws<InvalidOperationException>(() => new ReadOnlyShipLoader(_registry, Current, Scratch).Load(world, ship));
    }

    [Fact]
    public void Framing_UsesBoundsCenterAndHalfDiagonal()
    {
        var framing = ShipFraming.FromBounds(new Vector3(0, 0, 0), new Vector3(4, 2, 2));

        Assert.Equal(new Vector3(2, 1, 1), framing.Center);
        Assert.Equal(MathF.Sqrt(24f) * 0.5f, framing.Radius, 4);
    }

    [Fact]
    public void Framing_EmptyOrTinyShips_KeepMinimumRadius()
    {
        Assert.Equal(ShipFraming.MinRadius, ShipFraming.FromStats(default).Radius);
        Assert.Equal(ShipFraming.MinRadius, ShipFraming.FromBounds(Vector3.Zero, new Vector3(0.5f)).Radius);
    }

    [Theory]
    [InlineData(16f / 9f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    public void Framing_CameraFitShowsTheWholeShip(float aspect)
    {
        var stats = new ShipBlockStats(4, 1000, new Vector3i(-40, 0, 10), new Vector3i(24, 18, 90));
        var framing = ShipFraming.FromStats(stats);
        var camera = new OrbitViewCamera { AspectRatio = aspect };

        camera.Frame(framing.Center, framing.Radius);

        float distance = (camera.Position - framing.Center).Length;
        float halfAngle = OrbitViewCamera.FitHalfAngle(MathHelper.DegreesToRadians(camera.FOV) * 0.5f, aspect);
        Assert.True(distance * MathF.Sin(halfAngle) >= framing.Radius);
        Assert.True(camera.DepthFar > distance + framing.Radius);
        Assert.True(camera.DepthNear < distance - framing.Radius);
    }
}
