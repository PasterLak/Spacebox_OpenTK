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

        Assert.Equal("Blocks/new_block/new_block.json", added.File.RelativePath);
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "new_block", "new_block.json")));
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
    public void Blocks_DuplicateUsesASafeFolderNameFromTheShortId()
    {
        _sets.Write("Mod/Blocks/stone/stone.json", "{ \"ID\": \"mod:stone\" }");
        _sets.Write("Mod/Blocks/evil/evil.json", "{ \"ID\": \"other:../Evil\\\\x\" }");
        var project = new GameSetProject(Root, "mod");
        var source = GameSetSources.Blocks(project);
        var entries = source.GetEntries().ToArray();

        var stone = source.Duplicate(entries.Single(e => GameSetCatalog.Blocks.GetId(e.Node) == "mod:stone"));
        var evil = source.Duplicate(entries.Single(e => GameSetCatalog.Blocks.GetId(e.Node) != "mod:stone"));
        project.SaveAll();

        Assert.Equal("Blocks/stone_2/stone_2.json", stone.File.RelativePath);
        Assert.Equal("mod:stone_2", GameSetCatalog.Blocks.GetId(stone.Node));
        Assert.Equal("Blocks/evil_x_2/evil_x_2.json", evil.File.RelativePath);
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "evil_x_2", "evil_x_2.json")));
    }

    [Fact]
    public void Project_RefusesFilesOutsideTheGameSet()
    {
        var project = new GameSetProject(Root, "mod");

        Assert.Throws<ArgumentException>(() => project.CreateFile("../outside.json", new JsonObject()));
    }

    [Fact]
    public void Blueprints_DuplicateOfAnEntryBlueprintGetsTheEntryAsProduct()
    {
        _sets.Write("Mod/Items/bulb/item.json", "{ \"ID\": \"bulb\" }");
        _sets.Write("Mod/Items/bulb/blueprint.json", "{ \"Ingredients\": [] }");
        var project = new GameSetProject(Root, "mod");
        var source = GameSetSources.Blueprints(project);

        var copy = source.Duplicate(Assert.Single(source.GetEntries()));

        Assert.Equal("bulb", copy.Node!["Product"]!["Item"]!.GetValue<string>());
        Assert.Null(source.GetEntries().First(e => e.File.RelativePath.EndsWith("blueprint.json")).Node!["Product"]);
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

    [Fact]
    public void Items_RemoveDeletesEntryFilesAndOrderId()
    {
        _sets.Write("Mod/Items/bulb/item.json", "{ \"ID\": \"bulb\" }");
        _sets.Write("Mod/Items/bulb/blueprint.json", "{ \"Product\": { \"Item\": \"bulb\", \"Quantity\": 1 } }");
        _sets.Write("Mod/Items/bulb/recipes.json", "[ ]");
        _sets.Write("Mod/Items/rod/item.json", "{ \"ID\": \"rod\" }");
        _sets.Write("Mod/Items/order.json", "[ \"bulb\", \"rod\" ]");
        var project = new GameSetProject(Root, "mod");
        var source = GameSetSources.Items(project);

        source.Remove(source.GetEntries().First(e => GameSetCatalog.Items.GetId(e.Node) == "bulb"));
        project.SaveAll();

        Assert.False(File.Exists(Path.Combine(Root, "Items", "bulb", "item.json")));
        Assert.False(File.Exists(Path.Combine(Root, "Items", "bulb", "blueprint.json")));
        Assert.False(File.Exists(Path.Combine(Root, "Items", "bulb", "recipes.json")));
        Assert.Equal("[\"rod\"]", EditorJsonFile.Parse(File.ReadAllText(Path.Combine(Root, "Items", "order.json")))!.ToJsonString());
    }

    [Fact]
    public void Blueprints_RemoveKeepsTheItemEntry()
    {
        _sets.Write("Mod/Items/bulb/item.json", "{ \"ID\": \"bulb\" }");
        _sets.Write("Mod/Items/bulb/blueprint.json", "{ \"Product\": { \"Item\": \"bulb\", \"Quantity\": 1 } }");
        _sets.Write("Mod/Items/bulb/recipes.json", "[ ]");
        _sets.Write("Mod/Items/order.json", "[ \"bulb\" ]");
        var project = new GameSetProject(Root, "mod");
        var source = GameSetSources.Blueprints(project);

        source.Remove(source.GetEntries().Single());
        project.SaveAll();

        Assert.False(File.Exists(Path.Combine(Root, "Items", "bulb", "blueprint.json")));
        Assert.True(File.Exists(Path.Combine(Root, "Items", "bulb", "item.json")));
        Assert.True(File.Exists(Path.Combine(Root, "Items", "bulb", "recipes.json")));
    }
}
