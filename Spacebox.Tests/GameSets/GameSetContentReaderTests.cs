using System.Text.Json.Nodes;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class GameSetContentReaderTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private static string[] Ids(IReadOnlyList<ContentEntry> entries) => entries.Select(e => e.Key).ToArray();

    [Fact]
    public void FlatFormat_ReadsAllContentInFileOrder()
    {
        _sets.Write("Base/blocks.json", "[ { \"ID\": \"stone\", \"Name\": \"Stone\" }, { \"ID\": \"iron\", \"Name\": \"Iron\" } ]");
        _sets.Write("Base/items.json", "[ { \"ID\": \"ingot\", \"Name\": \"Ingot\" }, ]");
        _sets.Write("Base/crafting.json", "{ \"Categories\": [ { \"Id\": \"building\" } ], \"Blueprints\": [ { \"Ingredients\": [ { \"Item\": \"ingot\", \"Quantity\": 2 } ], \"Product\": { \"Item\": \"iron\", \"Quantity\": 1 } } ] }");
        _sets.Write("Base/recipes.json", "{ \"RecipeCatalog\": [ { \"Type\": \"furnace\", \"Recipes\": [ { \"Ingredient\": { \"Item\": \"stone\", \"Quantity\": 1 }, \"Product\": { \"Item\": \"ingot\", \"Quantity\": 1 } } ] } ] }");

        var content = GameSetContentReader.ReadFolder(Path.Combine(_sets.Root, "Base"));

        Assert.Equal(new[] { "stone", "iron" }, Ids(content.Blocks));
        Assert.Equal(new[] { "ingot" }, Ids(content.Items));
        Assert.Equal(new[] { "building" }, Ids(content.Categories));
        Assert.Equal(new[] { "iron" }, Ids(content.Blueprints));
        Assert.Equal(new[] { "furnace|stone" }, Ids(content.Recipes));
        Assert.Equal("furnace", ContentJson.GetString(content.Recipes[0].Data, "Type"));
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void FolderFormat_ReadsEntriesWithOrderBlueprintsAndRecipes()
    {
        _sets.Write("Base/Blocks/Rocks/stone/block.json", "{ \"ID\": \"stone\", \"Name\": \"Stone\" }");
        _sets.Write("Base/Blocks/Metal/iron/block.json", "{ \"ID\": \"iron\", \"Name\": \"Iron\" }");
        _sets.Write("Base/Blocks/Metal/iron/blueprint.json", "{ \"Ingredients\": [ { \"Item\": \"ingot\", \"Quantity\": 2 } ] }");
        _sets.Write("Base/Blocks/Rocks/stone/recipes.json", "[ { \"Type\": \"furnace\", \"Product\": { \"Item\": \"ingot\", \"Quantity\": 1 } } ]");
        _sets.Write("Base/Blocks/order.json", "[ \"stone\", \"iron\" ]");
        _sets.Write("Base/Items/ingot/item.json", "{ \"ID\": \"ingot\", \"Name\": \"Ingot\" }");
        _sets.Write("Base/Crafting/categories.json", "{ \"Categories\": [ { \"Id\": \"building\" } ] }");
        _sets.Write("Base/Recipes/crusher.json", "{ \"Type\": \"crusher\", \"Recipes\": [ { \"Ingredient\": { \"Item\": \"iron\" }, \"Product\": { \"Item\": \"ingot\" } } ] }");

        var content = GameSetContentReader.ReadFolder(Path.Combine(_sets.Root, "Base"));

        Assert.Equal(new[] { "stone", "iron" }, Ids(content.Blocks));
        Assert.Equal(new[] { "ingot" }, Ids(content.Items));
        Assert.Equal(new[] { "building" }, Ids(content.Categories));

        var blueprint = Assert.Single(content.Blueprints);
        Assert.Equal("iron", blueprint.Key);
        Assert.Equal(1, (int)blueprint.Data["Product"]!["Quantity"]!);

        Assert.Equal(new[] { "crusher|iron", "furnace|stone" }, Ids(content.Recipes));
        Assert.Equal("stone", ContentJson.GetString((JsonObject)content.Recipes[1].Data["Ingredient"]!, "Item"));
        Assert.EndsWith("stone", content.Blocks[0].SearchDirectories[0]);
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void FolderFormat_EntriesNotInOrderFileFollowSortedByPath()
    {
        _sets.Write("Base/Blocks/b_block/block.json", "{ \"ID\": \"b\" }");
        _sets.Write("Base/Blocks/a_block/block.json", "{ \"ID\": \"a\" }");
        _sets.Write("Base/Blocks/c_block/block.json", "{ \"ID\": \"c\" }");
        _sets.Write("Base/Blocks/order.json", "[ \"c\" ]");

        var content = GameSetContentReader.ReadFolder(Path.Combine(_sets.Root, "Base"));

        Assert.Equal(new[] { "c", "a", "b" }, Ids(content.Blocks));
    }

    [Fact]
    public void MixedFormats_FlatEntriesComeFirstAndDuplicatesKeepTheFirst()
    {
        _sets.Write("Base/blocks.json", "[ { \"ID\": \"stone\", \"Name\": \"Flat\" } ]");
        _sets.Write("Base/Blocks/stone/block.json", "{ \"ID\": \"stone\", \"Name\": \"Folder\" }");
        _sets.Write("Base/Blocks/iron/block.json", "{ \"ID\": \"iron\", \"Name\": \"Iron\" }");

        var content = GameSetContentReader.ReadFolder(Path.Combine(_sets.Root, "Base"));

        Assert.Equal(new[] { "stone", "iron" }, Ids(content.Blocks));
        Assert.Equal("Flat", ContentJson.GetString(content.Blocks[0].Data, "Name"));
        Assert.Contains(content.Warnings, w => w.Contains("'stone'"));
    }

    [Fact]
    public void IgnoredAndEmissionFoldersAreNotEntries()
    {
        _sets.Write("Base/Blocks/_old/stone/block.json", "{ \"ID\": \"old\" }");
        _sets.Write("Base/Blocks/iron/block.json", "{ \"ID\": \"iron\" }");
        _sets.Write("Base/Blocks/iron/Emissions/block.json", "{ \"ID\": \"wrong\" }");

        var content = GameSetContentReader.ReadFolder(Path.Combine(_sets.Root, "Base"));

        Assert.Equal(new[] { "iron" }, Ids(content.Blocks));
    }

    [Fact]
    public void Extension_PatchesExistingEntriesInPlaceAndAppendsNewOnes()
    {
        _sets.Write("Base/blocks.json", "[ { \"ID\": \"stone\", \"Name\": \"Stone\", \"Mass\": 2, \"Textures\": { \"Active\": { \"All\": \"stone\", \"Up\": \"top\" } }, \"Tags\": [ \"a\", \"b\" ] }, { \"ID\": \"iron\", \"Name\": \"Iron\" } ]");
        _sets.Write("Ext/Blocks/stone/block.json", "{ \"ID\": \"stone\", \"mass\": 5, \"Textures\": { \"Active\": { \"All\": \"stone2\" } }, \"Tags\": [ \"c\" ] }");
        _sets.Write("Ext/Blocks/lamp/block.json", "{ \"ID\": \"lamp\", \"Name\": \"Lamp\" }");

        var layers = new[] { new ContentLayer("base", Path.Combine(_sets.Root, "Base")), new ContentLayer("ext", Path.Combine(_sets.Root, "Ext")) };
        var content = GameSetContentReader.Read(layers);

        Assert.Equal(new[] { "stone", "iron", "lamp" }, Ids(content.Blocks));
        var stone = content.Blocks[0].Data;
        Assert.Equal("Stone", ContentJson.GetString(stone, "Name"));
        Assert.Equal(5, (int)stone["Mass"]!);
        Assert.Equal("stone2", stone["Textures"]!["Active"]!["All"]!.GetValue<string>());
        Assert.Equal("top", stone["Textures"]!["Active"]!["Up"]!.GetValue<string>());
        Assert.Equal(new[] { "c" }, stone["Tags"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray());
        Assert.Same(layers[1], content.Blocks[0].Layer);
        Assert.EndsWith(Path.Combine("Ext", "Blocks", "stone"), content.Blocks[0].SearchDirectories[0]);
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void Extension_OverridesRecipesBlueprintsAndCategoriesByKey()
    {
        _sets.Write("Base/crafting.json", "{ \"Categories\": [ { \"Id\": \"tools\", \"Name\": \"Tools\", \"Row\": 1 } ], \"Blueprints\": [ { \"Ingredients\": [ { \"Item\": \"a\", \"Quantity\": 1 } ], \"Product\": { \"Item\": \"drill\", \"Quantity\": 1 } } ] }");
        _sets.Write("Base/recipes.json", "{ \"RecipeCatalog\": [ { \"Type\": \"furnace\", \"Recipes\": [ { \"DurationSec\": 5, \"Ingredient\": { \"Item\": \"ore\" }, \"Product\": { \"Item\": \"ingot\" } }, { \"Ingredient\": { \"Item\": \"sand\" }, \"Product\": { \"Item\": \"glass\" } } ] } ] }");
        _sets.Write("Ext/Crafting/more.json", "{ \"Categories\": [ { \"Id\": \"tools\", \"Row\": 2 } ], \"Blueprints\": [ { \"Ingredients\": [ { \"Item\": \"b\", \"Quantity\": 3 } ], \"Product\": { \"Item\": \"drill\", \"Quantity\": 1 } } ] }");
        _sets.Write("Ext/Recipes/furnace.json", "{ \"Type\": \"Furnace\", \"Recipes\": [ { \"DurationSec\": 1, \"Ingredient\": { \"Item\": \"ORE\" }, \"Product\": { \"Item\": \"ingot\" } } ] }");

        var content = GameSetContentReader.Read(new[] { new ContentLayer("base", Path.Combine(_sets.Root, "Base")), new ContentLayer("ext", Path.Combine(_sets.Root, "Ext")) });

        var category = Assert.Single(content.Categories).Data;
        Assert.Equal("Tools", ContentJson.GetString(category, "Name"));
        Assert.Equal(2, (int)category["Row"]!);

        var blueprint = Assert.Single(content.Blueprints).Data;
        var ingredient = Assert.Single(blueprint["Ingredients"]!.AsArray());
        Assert.Equal("b", ingredient!["Item"]!.GetValue<string>());

        Assert.Equal(new[] { "furnace|ore", "furnace|sand" }, Ids(content.Recipes));
        Assert.Equal(1, (int)content.Recipes[0].Data["DurationSec"]!);
    }

    [Fact]
    public void BaseWithoutContent_FallsBackToDefaultPerKind()
    {
        _sets.Write("Default/blocks.json", "[ { \"ID\": \"stone\" } ]");
        _sets.Write("Default/items.json", "[ { \"ID\": \"ingot\" } ]");
        _sets.Write("Mod/items.json", "[ { \"ID\": \"gem\" } ]");

        var content = GameSetContentReader.Read(new[] { new ContentLayer("mod", Path.Combine(_sets.Root, "Mod")) }, new ContentLayer("default", Path.Combine(_sets.Root, "Default")));

        Assert.Equal(new[] { "stone" }, Ids(content.Blocks));
        Assert.Equal(new[] { "gem" }, Ids(content.Items));
    }

    [Fact]
    public void ToFlatJson_GroupsRecipesByType()
    {
        _sets.Write("Base/Recipes/a.json", "{ \"Type\": \"crusher\", \"Recipes\": [ { \"Ingredient\": { \"Item\": \"x\" } } ] }");
        _sets.Write("Base/Recipes/b.json", "{ \"Type\": \"furnace\", \"Recipes\": [ { \"Ingredient\": { \"Item\": \"y\" } }, { \"Ingredient\": { \"Item\": \"z\" } } ] }");

        var recipes = GameSetContentReader.ReadFolder(Path.Combine(_sets.Root, "Base")).RecipesJson();

        var catalog = recipes["RecipeCatalog"]!.AsArray();
        Assert.Equal(2, catalog.Count);
        Assert.Equal("furnace", catalog[1]!["Type"]!.GetValue<string>());
        Assert.Equal(2, catalog[1]!["Recipes"]!.AsArray().Count);
    }

    [Fact]
    public void BrokenEntryJson_IsReportedAndSkipped()
    {
        _sets.Write("Base/Blocks/bad/block.json", "{ \"ID\": \"bad\" ");
        _sets.Write("Base/Blocks/good/block.json", "{ \"ID\": \"good\" }");

        var content = GameSetContentReader.ReadFolder(Path.Combine(_sets.Root, "Base"));

        Assert.Contains("good", Ids(content.Blocks));
        Assert.NotEmpty(content.Warnings);
    }
}
