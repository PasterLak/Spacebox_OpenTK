using System.Text.Json.Nodes;
using Spacebox.Editor.Data;
using Spacebox.Game.Resource.Content;
using Spacebox.Tests.GameSets;

namespace Spacebox.Tests.Editor;

public sealed class FolderFormatSourcesTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private string Root => Path.Combine(_sets.Root, "Mod");

    [Fact]
    public void Blocks_ShowsFlatAndFolderEntriesInLoadOrder()
    {
        _sets.Write("Mod/blocks.json", "[ { \"ID\": \"flat\" } ]");
        _sets.Write("Mod/Blocks/b/block.json", "{ \"ID\": \"b\" }");
        _sets.Write("Mod/Blocks/a/block.json", "{ \"ID\": \"a\" }");
        _sets.Write("Mod/Blocks/order.json", "[ \"b\", \"a\" ]");
        var source = GameSetSources.Blocks(new GameSetProject(Root, "mod"));

        var ids = source.GetEntries().Select(e => GameSetCatalog.Blocks.GetId(e.Node)).ToArray();

        Assert.Equal(new[] { "flat", "b", "a" }, ids);
    }

    [Fact]
    public void Blocks_AddCreatesAnEntryFolderWhenThereIsNoFlatFile()
    {
        _sets.Write("Mod/Blocks/stone/block.json", "{ \"ID\": \"stone\" }");
        _sets.Write("Mod/Blocks/order.json", "[ \"stone\" ]");
        var project = new GameSetProject(Root, "mod");
        var source = GameSetSources.Blocks(project);

        var added = source.Add(null);
        project.SaveAll();

        Assert.Equal("Blocks/new_block/block.json", added.File.RelativePath);
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "new_block", "block.json")));
        Assert.Equal(new[] { "stone", "new_block" }, GameSetContentReader.ReadFolder(Root).Blocks.Select(b => b.Key));
    }

    [Fact]
    public void Blocks_AddGoesToTheFlatFileForOldGameSets()
    {
        _sets.Write("Mod/blocks.json", "[ { \"ID\": \"stone\" } ]");
        var project = new GameSetProject(Root, "mod");

        var added = GameSetSources.Blocks(project).Add(null);

        Assert.Equal("blocks.json", added.File.RelativePath);
    }

    [Fact]
    public void Blueprints_IncludeEntryBlueprintsAndDuplicatesGoToTheCraftingFolder()
    {
        _sets.Write("Mod/Items/bulb/item.json", "{ \"ID\": \"bulb\" }");
        _sets.Write("Mod/Items/bulb/blueprint.json", "{ \"Product\": { \"Item\": \"bulb\", \"Quantity\": 1 }, \"Ingredients\": [] }");
        var project = new GameSetProject(Root, "mod");
        var source = GameSetSources.Blueprints(project);

        var entry = Assert.Single(source.GetEntries());
        var copy = source.Duplicate(entry);
        project.SaveAll();

        Assert.Equal("Crafting/blueprints.json", copy.File.RelativePath);
        Assert.Equal(2, source.GetEntries().Count);
        Assert.Equal("bulb", JsonNode.Parse(File.ReadAllText(Path.Combine(Root, "Crafting", "blueprints.json")))!["Blueprints"]![0]!["Product"]!["Item"]!.GetValue<string>());
    }

    [Fact]
    public void ProcessingRecipes_UseOneFilePerMachineType()
    {
        _sets.Write("Mod/Recipes/furnace.json", "{ \"Type\": \"furnace\", \"Recipes\": [ { \"Ingredient\": { \"Item\": \"ore\" } } ] }");
        var project = new GameSetProject(Root, "mod");
        var source = GameSetSources.ProcessingRecipes(project);

        Assert.Equal(new[] { "furnace" }, source.Groups);
        source.Add("crusher");
        project.SaveAll();

        Assert.True(File.Exists(Path.Combine(Root, "Recipes", "crusher.json")));
        Assert.Equal(2, GameSetContentReader.ReadFolder(Root).Recipes.Count);
    }
}
