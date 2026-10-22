using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.GameMath;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;
using Xunit;

namespace Spacebox.Tests.Versioning;

public class EntityStorageLayoutTests : IDisposable
{
    private const string DnaSample = "default:dna_sample";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_entity_layout_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string Build(Action<WorldFixtureBuilder> setup)
    {
        var builder = new WorldFixtureBuilder(Path.Combine(_root, "Worlds", "World"));
        setup(builder);
        return builder.Build();
    }

    private WorldUpgradeResult Upgrade(string folder, string version)
    {
        var info = new WorldInfo { Name = "W", GameVersion = version, ModId = "default" };
        return new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups"))
            .Upgrade(info, folder, Engine.Application.Version, GameSetIdIndex.Load(WorldToolkitTests.DefaultGameSet(), "default"));
    }

    private static string LegacyEntityPath(string folder, string name)
    {
        return EntityFiles.PathOf(Path.Combine(folder, WorldFileLayout.SectorsFolder, WorldFixtureBuilder.SectorFolderName), name);
    }

    [Fact]
    public void Upgrade_012_MovesEntitiesOutOfSectorFoldersAndIndexesThem()
    {
        string folder = Build(w => w.Version("0.1.2")
            .Chunk(new[] { "default:air", "default:light_rock" })
            .Chunk(new[] { "default:light_rock" }, entity: "2"));

        Assert.Equal(new[] { "Sectors/Sector+0+0+0/1.entity", "Sectors/Sector+0+0+0/2.entity" }, WorldInspector.EntityFiles(folder));

        var result = Upgrade(folder, "0.1.2");

        Assert.True(result.Success, result.Error);
        Assert.Empty(result.Warnings);
        Assert.Equal(new[] { "Entities/1.entity", "Entities/2.entity" }, WorldInspector.EntityFiles(folder));
        Assert.True(File.Exists(Path.Combine(folder, WorldFileLayout.SectorsFolder, WorldFixtureBuilder.SectorFolderName, WorldFixtureBuilder.SectorFolderName + WorldFileLayout.SectorExtension)));

        var index = WorldInspector.EntityIndex(folder).OrderBy(r => r.Name).ToList();
        Assert.Equal(new[] { "1", "2" }, index.Select(r => r.Name));
        Assert.All(index, r => Assert.Equal(Vector3i.Zero, r.Sector));
        Assert.All(index, r => Assert.Equal(WorldFixtureBuilder.EntityPosition, r.Local));

        var entity = NbtFileEditor.Read(EntityFiles.PathInWorld(folder, "1"));
        Assert.True(EntityTagLocation.TryReadSector(entity, out var sector));
        Assert.Equal(Vector3i.Zero, sector);
    }

    [Fact]
    public void Upgrade_012_RegistersDnaReconstructorsWithTheirDna()
    {
        var cell = new Vector3i(3, 4, 5);
        string folder = Build(w => w.Version("0.1.2").SparseChunk("1", EntityStorageLayout.ReconstructorIds[0], new[] { cell }, DnaSample, 3));

        var result = Upgrade(folder, "0.1.2");

        Assert.True(result.Success, result.Error);
        Assert.Empty(result.Warnings);
        var record = Assert.Single(WorldInspector.Reconstructors(folder));
        Assert.Equal(-1, record.Key.EntityId);
        Assert.Equal(cell, record.Key.Cell);
        Assert.Equal(3, record.DnaCount);
        Assert.True(record.IsPowered);
        Assert.Equal(WorldFixtureBuilder.EntityPosition + new Vector3(3.5f, 4.5f, 5.5f), record.WorldPosition);
    }

    [Fact]
    public void UpgradeInPlace_MigratesWorldsSavedWithTheOldLayoutAtTheCurrentVersion()
    {
        string folder = Build(w => w.LegacyEntityLayout().Chunk(new[] { "default:light_rock" }));
        Assert.True(EntityStorageLayout.HasLegacyFiles(folder));

        string backup = EntityStorageLayout.UpgradeInPlace(folder, Path.Combine(_root, "backups"));

        Assert.False(EntityStorageLayout.HasLegacyFiles(folder));
        Assert.Equal(new[] { "Entities/1.entity" }, WorldInspector.EntityFiles(folder));
        Assert.Single(WorldInspector.EntityIndex(folder));
        Assert.True(File.Exists(backup));
        Assert.Contains(EntityStorageLayout.BackupLabel, Path.GetFileName(backup));
        Assert.Null(EntityStorageLayout.UpgradeInPlace(folder, Path.Combine(_root, "backups")));
    }

    [Fact]
    public void CurrentWorlds_AreBuiltWithTheEntityFolder()
    {
        string folder = Build(w => w.Chunk(new[] { "default:light_rock" }));

        Assert.False(EntityStorageLayout.HasLegacyFiles(folder));
        Assert.Equal(new[] { "Entities/1.entity" }, WorldInspector.EntityFiles(folder));
        Assert.Equal(WorldUpgradeStatus.UpToDate, Upgrade(folder, Engine.Application.Version).Status);
    }

    [Fact]
    public void Migration_KeepsTheNewerFileWhenAnEntityIsSavedInTwoSectors()
    {
        string folder = Build(w => w.LegacyEntityLayout().Chunk(new[] { "default:light_rock" }));
        string original = LegacyEntityPath(folder, "1");
        string otherSector = Path.Combine(folder, WorldFileLayout.SectorsFolder, SpaceMath.Sector.IndexToFolderName(new Vector3i(1, -2, 0)));
        Directory.CreateDirectory(otherSector);
        string copy = EntityFiles.PathOf(otherSector, "1");
        File.Copy(original, copy);
        File.SetLastWriteTimeUtc(original, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(copy, DateTime.UtcNow);

        EntityStorageLayout.UpgradeInPlace(folder, Path.Combine(_root, "backups"));

        Assert.Equal(new[] { "Entities/1.entity" }, WorldInspector.EntityFiles(folder));
        var record = Assert.Single(WorldInspector.EntityIndex(folder));
        Assert.Equal(new Vector3i(1, -2, 0), record.Sector);
    }

    [Theory]
    [InlineData("Sector+0+0+0", 0, 0, 0)]
    [InlineData("Sector-3+12-7", -3, 12, -7)]
    public void SectorFolderName_ParsesWhatTheGameWrites(string name, int x, int y, int z)
    {
        Assert.True(SectorFolderName.TryParse(name, out var index));
        Assert.Equal(new Vector3i(x, y, z), index);
        Assert.Equal(name, SpaceMath.Sector.IndexToFolderName(index));
    }

    [Theory]
    [InlineData("Sector")]
    [InlineData("Sector+1+2")]
    [InlineData("backup")]
    public void SectorFolderName_RejectsOtherFolders(string name)
    {
        Assert.False(SectorFolderName.TryParse(name, out _));
    }
}
