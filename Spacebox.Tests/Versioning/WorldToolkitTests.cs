using SharpNBT;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;
using Xunit;

namespace Spacebox.Tests.Versioning;

public class WorldToolkitTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_toolkit_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string World(Action<WorldFixtureBuilder> setup)
    {
        var builder = new WorldFixtureBuilder(Path.Combine(_root, "World"));
        setup(builder);
        return builder.Build();
    }

    private static WorldUpgradeContext Context(string folder) => new WorldUpgradeContext(new WorldInfo { Name = "W", GameVersion = "0.1.5" }, folder);

    internal static string DefaultGameSet()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", "GameSets");
            if (Directory.Exists(Path.Combine(candidate, "Default"))) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("GameSets");
    }

    [Fact]
    public void RenameBlock_UpdatesBlockAndItemPalettesPlayersAndDrops()
    {
        string folder = World(w => w
            .Chunk(new[] { "default:air", "default:old_rock" }, new[] { "default:old_rock", "default:power" })
            .Player("default:old_rock", 5)
            .Player("default:power", 1, "players/Bob.json")
            .Drop("default:old_rock", 3));

        var context = Context(folder);
        context.RenameBlock("default:old_rock", "default:light_rock");

        Assert.Equal(new[] { "default:air", "default:light_rock" }, WorldInspector.BlockPalettes(folder));
        Assert.Equal(new[] { "default:light_rock", "default:power" }, WorldInspector.ItemPalettes(folder));
        Assert.Contains("default:light_rock", WorldInspector.PlayerItems(folder));
        Assert.Contains("default:power", WorldInspector.PlayerItems(folder));
        Assert.Equal(new[] { "default:light_rock" }, WorldInspector.DropItems(folder));
        Assert.Equal(1, context.Counters["block palette entries"]);
        Assert.Contains("block palette entries", context.Summary());
    }

    [Fact]
    public void RenameItem_LeavesBlockPalettesAlone()
    {
        string folder = World(w => w.Chunk(new[] { "default:power" }, new[] { "default:power" }).Player("default:power"));

        Context(folder).RenameItem("default:power", "default:power_cell");

        Assert.Equal(new[] { "default:power" }, WorldInspector.BlockPalettes(folder));
        Assert.Equal(new[] { "default:power_cell" }, WorldInspector.ItemPalettes(folder));
        Assert.Equal(new[] { "default:power_cell" }, WorldInspector.PlayerItems(folder));
    }

    [Fact]
    public void RemoveBlock_BecomesAirAndItsItemsDisappear()
    {
        string folder = World(w => w
            .Chunk(new[] { "default:test_block" }, new[] { "default:test_block" })
            .Player("default:test_block").Player("default:power")
            .Drop("default:test_block"));

        Context(folder).RemoveBlock("default:test_block");

        Assert.Equal(new[] { WorldUpgradeContext.Air }, WorldInspector.BlockPalettes(folder));
        Assert.Equal(new[] { WorldUpgradeContext.RemovedItem }, WorldInspector.ItemPalettes(folder));
        Assert.Equal(new[] { "default:power" }, WorldInspector.PlayerItems(folder));
        Assert.Empty(WorldInspector.DropItems(folder));
    }

    [Fact]
    public void RemoveItem_CanUseAReplacement()
    {
        string folder = World(w => w.Chunk(new[] { "default:air" }, new[] { "default:beer" }).Player("default:beer"));

        Context(folder).RemoveItem("default:beer", "default:water");

        Assert.Equal(new[] { "default:water" }, WorldInspector.ItemPalettes(folder));
        Assert.Equal(new[] { "default:water" }, WorldInspector.PlayerItems(folder));
    }

    [Fact]
    public void Edits_DoNotRewriteFilesWithoutChanges()
    {
        string folder = World(w => w.Chunk(new[] { "default:air" }).Chunk(new[] { "default:light_rock" }, entity: "2").Player("default:power"));
        var before = WorldInspector.Snapshot(folder);

        var context = Context(folder);
        context.RenameBlock("default:missing", "default:other");

        Assert.Equal(before, WorldInspector.Snapshot(folder));
        Assert.Equal("no file changes", context.Summary());
    }

    [Fact]
    public void EditChunks_GivesRawAccessAndRenameKeyKeepsValues()
    {
        string folder = World(w => w.Chunk(new[] { "default:air" }));

        int changed = Context(folder).EditChunks(chunk => NbtFileEditor.RenameKey(chunk, NBTKey.CHUNK.directions, "old_directions"));

        Assert.Equal(1, changed);
        var chunk = ChunkTagAccess.Chunks(NbtFileEditor.Read(WorldFileLayout.EntityFilePaths(folder).Single())).Single();
        Assert.False(chunk.ContainsKey(NBTKey.CHUNK.directions));
        Assert.True(chunk.ContainsKey("old_directions"));
        Assert.Equal(32 * 32 * 32, chunk.Get<ByteArrayTag>(NBTKey.CHUNK.blocks).ToArray().Length);
    }

    [Fact]
    public void Renamed_CopiesNestedTags()
    {
        var compound = new CompoundTag("old");
        var list = new ListTag("values", TagType.Int);
        list.Add(new IntTag(null, 7));
        compound.Add(list);
        compound.Add(new StringTag("text", "hi"));

        var copy = (CompoundTag)NbtFileEditor.Renamed(compound, "new");

        Assert.Equal("new", copy.Name);
        Assert.Equal(7, ((IntTag)copy.Get<ListTag>("values")[0]).Value);
        Assert.Equal("hi", copy.Get<StringTag>("text").Value);
    }

    [Fact]
    public void EditPlayersAndSectors_ReachEveryFile()
    {
        string folder = World(w => w.Chunk(new[] { "default:air" }).Player("default:power").Player("default:power", 1, "players/Bob.json"));
        var context = Context(folder);

        int players = context.EditPlayers(player =>
        {
            player["Shield"] = 10;
            return true;
        });
        int sectors = context.EditSectors(sector =>
        {
            sector.Add(new IntTag("version_marker", 1));
            return true;
        });

        Assert.Equal(2, players);
        Assert.Equal(1, sectors);
        Assert.True(NbtFileEditor.Read(WorldFileLayout.SectorFiles(folder).Single()).ContainsKey("version_marker"));
    }

    [Fact]
    public void IdRemap_ValidatesIdsAndTracksRemovals()
    {
        Assert.Throws<ArgumentException>(() => new IdRemap().Rename("light_rock", "default:x"));

        var remap = new IdRemap().Rename("default:a", "default:b").Remove("default:c", WorldUpgradeContext.Air);
        var items = remap.WithRemovalsAs(WorldUpgradeContext.RemovedItem);

        Assert.True(items.TryMap("default:a", out var renamed));
        Assert.Equal("default:b", renamed);
        Assert.True(items.TryMap("default:c", out var removed));
        Assert.Equal(WorldUpgradeContext.RemovedItem, removed);
        Assert.False(items.TryMap("default:z", out _));
        Assert.Equal(2, items.Changes);
    }

    [Fact]
    public void GameSetIdIndex_ReadsTheDefaultGameSet()
    {
        var ids = GameSetIdIndex.Load(DefaultGameSet(), "default");

        Assert.True(ids.IsBlock("default:aluminium_ore"));
        Assert.True(ids.IsBlock("default:air"));
        Assert.True(ids.IsItem("default:aluminium_drill"));
        Assert.True(ids.IsItem("default:aluminium_ore"));
        Assert.False(ids.IsBlock("default:aluminium_drill"));
        Assert.False(ids.IsItem("default:nothing"));
    }

    [Fact]
    public void Validator_ReportsUnknownIdsOnly()
    {
        string folder = World(w => w
            .Chunk(new[] { "default:air", "default:light_rock", "default:ghost_block" }, new[] { "default:power", WorldUpgradeContext.RemovedItem })
            .Player("default:ghost_item")
            .Drop("default:aluminium_ore"));

        var warnings = WorldValidator.FindUnknownIds(folder, GameSetIdIndex.Load(DefaultGameSet(), "default"));

        Assert.Equal(2, warnings.Count);
        Assert.Contains(warnings, w => w.Contains("unknown blocks") && w.Contains("default:ghost_block"));
        Assert.Contains(warnings, w => w.Contains("unknown items") && w.Contains("default:ghost_item"));
    }
}
