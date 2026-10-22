using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;
using Xunit;

namespace Spacebox.Tests.Generation;

public class EntityCatalogTests : IDisposable
{
    private readonly string _world = Path.Combine(Path.GetTempPath(), "spacebox_catalog_" + Guid.NewGuid().ToString("N"));

    public EntityCatalogTests()
    {
        Directory.CreateDirectory(EntityFiles.FolderOf(_world));
    }

    public void Dispose()
    {
        if (Directory.Exists(_world)) Directory.Delete(_world, true);
    }

    private static CompoundTag Entity(long id, Vector3i sector, Vector3 local)
    {
        var tag = new CompoundTag("SpaceEntity");
        tag.Add(new LongTag(NBTKey.ENTITY.id, id));
        EntityTagLocation.Write(tag, sector, local);
        return tag;
    }

    private void WriteEntity(string name, long id, Vector3i sector, Vector3 local)
    {
        NbtFileEditor.Write(EntityFiles.PathInWorld(_world, name), Entity(id, sector, local));
    }

    [Fact]
    public void Upsert_MovesTheEntityBetweenSectors()
    {
        var catalog = new EntityCatalog();
        catalog.Upsert(new EntityRecord(5, "5", new Vector3i(0, 0, 0), Vector3.One));
        catalog.Upsert(new EntityRecord(5, "5", new Vector3i(1, 0, 0), Vector3.One));

        var first = new List<EntityRecord>();
        var second = new List<EntityRecord>();
        catalog.CollectInSector(new Vector3i(0, 0, 0), first);
        catalog.CollectInSector(new Vector3i(1, 0, 0), second);

        Assert.Empty(first);
        Assert.Equal(5, Assert.Single(second).Id);
        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void RemoveByName_ForgetsTheEntity()
    {
        var catalog = new EntityCatalog();
        catalog.Upsert(new EntityRecord(-7, "ship", Vector3i.Zero, Vector3.Zero));

        Assert.True(catalog.RemoveByName("ship"));
        Assert.False(catalog.TryGet(-7, out _));
        Assert.False(catalog.RemoveByName("ship"));
    }

    [Fact]
    public void IndexFile_RoundTrips()
    {
        var catalog = new EntityCatalog();
        catalog.Upsert(new EntityRecord(1, "1", new Vector3i(-3, 2, 9), new Vector3(1.5f, 2.5f, 4096f)));
        catalog.Upsert(new EntityRecord(-2, "station", Vector3i.Zero, Vector3.Zero));

        var records = EntityCatalogFile.FromTag(EntityCatalogFile.ToTag(catalog.Snapshot())).OrderBy(r => r.Id).ToList();

        Assert.Equal(new EntityRecord(-2, "station", Vector3i.Zero, Vector3.Zero), records[0]);
        Assert.Equal(new EntityRecord(1, "1", new Vector3i(-3, 2, 9), new Vector3(1.5f, 2.5f, 4096f)), records[1]);
    }

    [Fact]
    public void Load_DropsIndexedEntitiesWithoutFileAndIndexesNewFiles()
    {
        WriteEntity("1", 1, new Vector3i(0, 0, 0), Vector3.One);
        var stale = new EntityCatalog();
        stale.Upsert(new EntityRecord(1, "1", new Vector3i(0, 0, 0), Vector3.One));
        stale.Upsert(new EntityRecord(2, "2", new Vector3i(0, 0, 0), Vector3.One));
        EntityCatalogFile.Write(_world, stale);
        WriteEntity("copied", 3, new Vector3i(4, 0, -1), new Vector3(7, 8, 9));

        var catalog = EntityCatalogFile.Load(_world, false);

        Assert.True(catalog.TryGet(1, out _));
        Assert.False(catalog.TryGet(2, out _));
        Assert.True(catalog.TryGet(3, out var copied));
        Assert.Equal(new EntityRecord(3, "copied", new Vector3i(4, 0, -1), new Vector3(7, 8, 9)), copied);
    }

    [Fact]
    public void Load_RebuildsAnUnreadableIndexFromTheFiles()
    {
        WriteEntity("1", 1, new Vector3i(2, 2, 2), Vector3.Zero);
        File.WriteAllText(EntityCatalogFile.PathOf(_world), "not nbt");

        var catalog = EntityCatalogFile.Load(_world, false);

        Assert.True(catalog.TryGet(1, out var record));
        Assert.Equal(new Vector3i(2, 2, 2), record.Sector);
    }

    [Fact]
    public void Load_IgnoresASecondFileWithTheSameId()
    {
        WriteEntity("a", 9, Vector3i.Zero, Vector3.Zero);
        WriteEntity("b", 9, Vector3i.Zero, Vector3.One);

        var catalog = EntityCatalogFile.Load(_world, false);

        Assert.Equal(1, catalog.Count);
    }

    [Fact]
    public void TagLocation_UsesTheSectorStoredInTheFile()
    {
        var tag = Entity(1, new Vector3i(1, 0, -1), new Vector3(5, 6, 7));

        var world = EntityTagLocation.ReadWorld(tag, new Vector3(999, 999, 999));

        Assert.Equal(new Vector3(8192 + 5, 6, -8192 + 7), world);
    }

    [Fact]
    public void TagLocation_FallsBackToTheLoadingSectorForOldFiles()
    {
        var tag = new CompoundTag("SpaceEntity");
        tag.Add(new FloatTag(NBTKey.ENTITY.local_x, 1f));
        tag.Add(new FloatTag(NBTKey.ENTITY.local_y, 2f));
        tag.Add(new FloatTag(NBTKey.ENTITY.local_z, 3f));

        Assert.False(EntityTagLocation.TryReadSector(tag, out _));
        Assert.Equal(new Vector3(8193, 2, 3), EntityTagLocation.ReadWorld(tag, new Vector3(8192, 0, 0)));
    }
}
