using SharpNBT;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;
using Xunit;

namespace Spacebox.Tests.Versioning;

public class WorldFormatProbeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_probe_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string Build012World()
    {
        return new WorldFixtureBuilder(Path.Combine(_root, "Worlds", "World")).Version("0.1.2")
            .Chunk(new[] { "default:air", "default:light_rock" })
            .Build();
    }

    private static void EditEntities(string folder, Func<CompoundTag, bool> edit)
    {
        foreach (var file in WorldFileLayout.EntityFilePaths(folder)) NbtFileEditor.Edit(file, edit);
    }

    private static void MakeWorldSpacePositions(string folder)
    {
        EditEntities(folder, entity => NbtFileEditor.RenameKey(entity, NBTKey.ENTITY.local_x, "world_x"));
    }

    private static void MakeLegacy(string folder)
    {
        EditEntities(folder, entity => NbtFileEditor.RenameKey(entity, NBTKey.ENTITY.local_x, "worldX"));
    }

    private WorldUpgrader Upgrader() => new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups"));

    [Fact]
    public void CurrentWorld_IsDetectedAsCurrent()
    {
        Assert.Equal(WorldSaveFormat.Current, WorldFormatProbe.Detect(Build012World()));
    }

    [Fact]
    public void WorldSpacePositions_AreDetected()
    {
        string folder = Build012World();
        MakeWorldSpacePositions(folder);

        Assert.Equal(WorldSaveFormat.WorldSpacePositions, WorldFormatProbe.Detect(folder));
    }

    [Fact]
    public void LegacyEntity_IsDetected()
    {
        string folder = Build012World();
        MakeLegacy(folder);

        Assert.Equal(WorldSaveFormat.Legacy, WorldFormatProbe.Detect(folder));
    }

    [Fact]
    public void LegacyChunkWithoutPalette_IsDetected()
    {
        string folder = Build012World();
        EditEntities(folder, entity =>
        {
            foreach (var chunk in ChunkTagAccess.Chunks(entity)) chunk.Remove(NBTKey.CHUNK.palette_blocks);
            return true;
        });

        Assert.Equal(WorldSaveFormat.Legacy, WorldFormatProbe.Detect(folder));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OldFormat012World_IsRefusedBeforeBackupAndUntouched(bool legacy)
    {
        string folder = Build012World();
        if (legacy) MakeLegacy(folder);
        else MakeWorldSpacePositions(folder);
        var before = WorldInspector.Snapshot(folder);
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.2" };

        var check = Upgrader().Check(info, folder, Engine.Application.Version, out var reason);
        var result = Upgrader().Upgrade(info, folder, Engine.Application.Version);

        Assert.Equal(WorldUpgradeStatus.UnsupportedFormat, check);
        Assert.False(string.IsNullOrEmpty(reason));
        Assert.Equal(WorldUpgradeStatus.UnsupportedFormat, result.Status);
        Assert.False(result.Success);
        Assert.Equal("0.1.2", info.GameVersion);
        Assert.Equal(before, WorldInspector.Snapshot(folder));
        Assert.False(Directory.Exists(Path.Combine(_root, "backups")));
    }

    [Fact]
    public void CurrentFormat012World_PassesTheCheck()
    {
        string folder = Build012World();

        var status = Upgrader().Check(new WorldInfo { Name = "W", GameVersion = "0.1.2" }, folder, Engine.Application.Version, out var reason);

        Assert.Equal(WorldUpgradeStatus.Ok, status);
        Assert.Null(reason);
    }

    [Fact]
    public void CorruptEntityFile_DoesNotBlockTheUpgrade()
    {
        string folder = Build012World();
        string goodFile = WorldFileLayout.EntityFilePaths(folder).First();
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(goodFile), "999" + Path.GetExtension(goodFile)), new byte[] { 0x1f, 0x8b, 1, 2, 3 });
        var info = new WorldInfo { Name = "W", GameVersion = "0.1.2" };

        var check = Upgrader().Check(info, folder, Engine.Application.Version, out var reason);
        var result = Upgrader().Upgrade(info, folder, Engine.Application.Version);

        Assert.Equal(WorldUpgradeStatus.Ok, check);
        Assert.True(result.Success, result.Error);
        Assert.Equal(Engine.Application.Version, info.GameVersion);
    }
}
