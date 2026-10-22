using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Xunit;

namespace Spacebox.Tests.Generation;

public class EntityCacheTests
{
    private static CompoundTag EntityTag(long id, Vector3 local)
    {
        var tag = new CompoundTag("Asteroid");
        tag.Add(new LongTag(NBTKey.ENTITY.id, id));
        tag.Add(new FloatTag(NBTKey.ENTITY.local_x, local.X));
        tag.Add(new FloatTag(NBTKey.ENTITY.local_y, local.Y));
        tag.Add(new FloatTag(NBTKey.ENTITY.local_z, local.Z));
        return tag;
    }

    [Fact]
    public void CachedEntity_IsListedOnlyForItsSector()
    {
        long id = 0x5EC70001;
        var sector = new Vector3i(3, 0, -2);
        WorldPersistenceManager.CacheEntityTag(id, EntityTag(id, new Vector3(10, 20, 30)), sector, "ship");
        try
        {
            var own = new List<NotGeneratedEntity>();
            var other = new List<NotGeneratedEntity>();
            WorldPersistenceManager.CollectCachedEntities(sector, own);
            WorldPersistenceManager.CollectCachedEntities(new Vector3i(0, 0, 0), other);

            var entry = Assert.Single(own, e => e.Id == id);
            Assert.Equal(new Vector3(10, 20, 30), entry.positionInSector);
            Assert.Equal("ship", entry.FileName);
            Assert.DoesNotContain(other, e => e.Id == id);

            Assert.True(WorldPersistenceManager.TryGetCachedEntity(id, sector, out _));
            Assert.False(WorldPersistenceManager.TryGetCachedEntity(id, new Vector3i(0, 0, 0), out _));
            Assert.True(WorldPersistenceManager.IsCachedInOtherSector(id, new Vector3i(0, 0, 0)));
            Assert.False(WorldPersistenceManager.IsCachedInOtherSector(id, sector));
        }
        finally
        {
            WorldPersistenceManager.ForgetEntity(id);
        }
    }

    [Fact]
    public void ForgottenEntity_IsNoLongerCached()
    {
        long id = 0x5EC70002;
        var sector = new Vector3i(1, 1, 1);
        WorldPersistenceManager.CacheEntityTag(id, EntityTag(id, Vector3.Zero), sector, "destroyed");

        WorldPersistenceManager.ForgetEntity(id);

        Assert.False(WorldPersistenceManager.TryGetCachedEntity(id, out _));
        var listed = new List<NotGeneratedEntity>();
        WorldPersistenceManager.CollectCachedEntities(sector, listed);
        Assert.DoesNotContain(listed, e => e.Id == id);
    }
}
