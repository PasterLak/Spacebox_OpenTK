using Spacebox.Game.GUI;
using Spacebox.Game.Versioning;
using Spacebox.Tests.Versioning;

namespace Spacebox.Tests.GameSets.Mock;

public sealed class MockWorldGameSetTests : IDisposable
{
    private static readonly string[] AddonBlocks = { "mock_base:air", "mock_base:mock_stone", "mock_addon:mock_lamp" };
    private static readonly string[] AddonItems = { "mock_addon:mock_bulb", "mock_base:mock_ingot" };

    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_mock_worlds_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string Build(string version, params string[] extensions)
    {
        return new WorldFixtureBuilder(Path.Combine(_root, "Worlds", "World"))
            .Version(version)
            .Mod("mock_base")
            .Extensions(extensions)
            .Chunk(AddonBlocks, AddonItems)
            .Player("mock_addon:mock_bulb")
            .Drop("mock_base:mock_ingot")
            .Build();
    }

    private static WorldInfo ReadInfo(string folder)
    {
        Assert.True(WorldInfoFiles.TryRead(folder, out var info));
        return info;
    }

    private static GameSetIdIndex IdsFor(WorldInfo info) => GameSetIdIndex.Load(MockGameSets.Folders, info.ModId, WorldGameSetCheck.ExtensionsOf(info));

    private WorldUpgradeResult Upgrade(WorldInfo info, string folder)
    {
        return new WorldUpgrader(WorldUpgradeRegistry.CreateDefault(), Path.Combine(_root, "backups")).Upgrade(info, folder, Engine.Application.Version, IdsFor(info));
    }

    [Fact]
    public void World012WithAnExtension_UpgradesAndKeepsTheExtensionIds()
    {
        string folder = Build("0.1.2", "mock_addon");
        var info = ReadInfo(folder);

        var result = Upgrade(info, folder);

        Assert.True(result.Success, result.Error);
        Assert.Equal(Engine.Application.Version, info.GameVersion);
        Assert.Equal(new[] { "mock_addon" }, info.Extensions);
        Assert.Empty(result.Warnings);
        Assert.Contains("mock_addon:mock_lamp", WorldInspector.BlockPalettes(folder));
        Assert.Contains("mock_addon:mock_bulb", WorldInspector.ItemPalettes(folder));
        Assert.Equal(new[] { "mock_addon:mock_bulb" }, WorldInspector.PlayerItems(folder));
    }

    [Fact]
    public void CurrentWorldWithAnExtension_IsUpToDateAndHasNoUnknownIds()
    {
        string folder = Build(Engine.Application.Version, "mock_addon");
        var info = ReadInfo(folder);

        var result = Upgrade(info, folder);

        Assert.Equal(WorldUpgradeStatus.UpToDate, result.Status);
        Assert.Empty(WorldValidator.FindUnknownIds(folder, IdsFor(info)));
    }

    [Fact]
    public void WorldWhoseExtensionIsMissing_ReportsUnknownIdsInsteadOfFailing()
    {
        string folder = Build("0.1.2", "mock_gone");
        var info = ReadInfo(folder);

        var result = Upgrade(info, folder);

        Assert.True(result.Success, result.Error);
        Assert.Contains(result.Warnings, w => w.Contains("unknown blocks") && w.Contains("mock_addon:mock_lamp"));
        Assert.Contains(result.Warnings, w => w.Contains("unknown items") && w.Contains("mock_addon:mock_bulb"));
        Assert.DoesNotContain(result.Warnings, w => w.Contains("mock_base:mock_stone"));
    }

    [Fact]
    public void WorldWithoutItsExtensionInTheList_ReportsTheExtensionIds()
    {
        string folder = Build(Engine.Application.Version);
        var info = ReadInfo(folder);

        var warnings = WorldValidator.FindUnknownIds(folder, IdsFor(info));

        Assert.Empty(info.Extensions);
        Assert.Equal(2, warnings.Count);
        Assert.Contains(warnings, w => w.StartsWith("1 unknown blocks: mock_addon:mock_lamp"));
        Assert.Contains(warnings, w => w.StartsWith("1 unknown items: mock_addon:mock_bulb"));
    }

    [Fact]
    public void IdIndex_ForAMissingBase_DoesNotThrowAndKnowsOnlyBuiltIns()
    {
        var ids = GameSetIdIndex.Load(MockGameSets.Folders, "mock_ghost_base", new[] { "mock_addon" });

        Assert.True(ids.IsBlock("mock_ghost_base:air"));
        Assert.False(ids.IsBlock("mock_addon:mock_lamp"));
    }

    [Fact]
    public void IdIndex_ContainsBaseExtensionAndChildIds()
    {
        var ids = GameSetIdIndex.Load(MockGameSets.Folders, "mock_base", new[] { "mock_child" });

        Assert.True(ids.IsBlock("mock_base:mock_stone"));
        Assert.True(ids.IsBlock("mock_addon:mock_lamp"));
        Assert.True(ids.IsItem("mock_addon:mock_bulb"));
        Assert.True(ids.IsItem("mock_child:mock_socket"));
        Assert.False(ids.IsBlock("mock_child:mock_lamp"));
    }
}
