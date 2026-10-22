using System.Text.Json.Nodes;
using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.Content.Conversion;

namespace Spacebox.Tests.GameSets;

public sealed class GameSetFolderConverterTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private string Root => Path.Combine(_sets.Root, "Mod");

    private void WriteFlatMod()
    {
        _sets.Config("Mod", "mod");
        _sets.Write("Mod/blocks.json", "[ { \"ID\": \"stone\", \"Name\": \"Stone\", \"Textures\": { \"Active\": { \"All\": \"stone\", \"Down\": \"shared\" } } }, { \"ID\": \"lamp\", \"Name\": \"Lamp\", \"Textures\": { \"Active\": { \"All\": \"lamp\", \"Down\": \"shared\" } } } ]");
        _sets.Write("Mod/items.json", "[ { \"ID\": \"ingot\", \"Name\": \"Ingot\", \"Sprite\": \"ingot\" } ]");
        _sets.Write("Mod/crafting.json", "{ \"Categories\": [ { \"Id\": \"building\" } ], \"Blueprints\": [ { \"Ingredients\": [ { \"Item\": \"ingot\", \"Quantity\": 2 } ], \"Product\": { \"Item\": \"lamp\", \"Quantity\": 1 } }, { \"Ingredients\": [ { \"Item\": \"ingot\", \"Quantity\": 1 } ], \"Product\": { \"Item\": \"eraser\", \"Quantity\": 1 } } ] }");
        _sets.Write("Mod/recipes.json", "{ \"RecipeCatalog\": [ { \"Type\": \"furnace\", \"Recipes\": [ { \"Ingredient\": { \"Item\": \"stone\", \"Quantity\": 1 }, \"Product\": { \"Item\": \"ingot\", \"Quantity\": 1 } } ] } ] }");
        _sets.Write("Mod/Blocks/Rocks/stone.png", "stone");
        _sets.Write("Mod/Blocks/lamp.png", "lamp");
        _sets.Write("Mod/Blocks/shared.png", "shared");
        _sets.Write("Mod/Items/Ingots/ingot.png", "ingot");
        _sets.Write("Mod/Emissions/lamp.png", "glow");
        _sets.Write("Mod/Emissions/ingot.png", "glow");
    }

    [Fact]
    public void Convert_MovesEntriesTexturesAndRecipesIntoFolders()
    {
        WriteFlatMod();
        var before = ContentSnapshot.Create(Root);

        var result = GameSetFolderConverter.Convert(Root);

        Assert.True(result.Success, string.Join("\n", result.Errors.Concat(result.Differences)));
        Assert.False(GameSetFolderConverter.HasFlatFiles(Root));
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "Rocks", "stone", "block.json")));
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "Rocks", "stone", "stone.png")));
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "lamp", "lamp.png")));
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "lamp", "Emissions", "lamp.png")));
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "shared.png")));
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "lamp", "blueprint.json")));
        Assert.True(File.Exists(Path.Combine(Root, "Items", "Ingots", "ingot", "item.json")));
        Assert.True(File.Exists(Path.Combine(Root, "Items", "Ingots", "ingot", "Emissions", "ingot.png")));
        Assert.True(File.Exists(Path.Combine(Root, "Crafting", "categories.json")));
        Assert.True(File.Exists(Path.Combine(Root, "Crafting", "blueprints.json")));
        Assert.True(File.Exists(Path.Combine(Root, "Recipes", "furnace.json")));
        Assert.False(Directory.Exists(Path.Combine(Root, "Emissions")));
        Assert.Empty(ContentSnapshot.Differences(before, ContentSnapshot.Create(Root)));
        Assert.False(Directory.Exists(Path.Combine(_sets.Root, "_Mod.converting")));
    }

    [Fact]
    public void Convert_KeepsEmissionsShared_WhenTheOtherAtlasUsesTheName()
    {
        WriteFlatMod();
        _sets.Write("Mod/Items/lamp.png", "item lamp");

        var result = GameSetFolderConverter.Convert(Root);

        Assert.True(result.Success, string.Join("\n", result.Errors.Concat(result.Differences)));
        Assert.True(File.Exists(Path.Combine(Root, "Emissions", "lamp.png")));
    }

    [Fact]
    public void Convert_FailsWithoutChanges_WhenAFileCannotBeConverted()
    {
        WriteFlatMod();
        _sets.Write("Mod/crafting.json", "{ \"Categories\": [], \"Blueprints\": [], \"Extra\": 1 }");

        var result = GameSetFolderConverter.Convert(Root);

        Assert.False(result.Success);
        Assert.True(File.Exists(Path.Combine(Root, "blocks.json")));
        Assert.True(File.Exists(Path.Combine(Root, "Blocks", "lamp.png")));
        Assert.False(Directory.Exists(Path.Combine(Root, "Blocks", "lamp")));
    }

    [Fact]
    public void Convert_RerunWithANewFlatFileUpdatesExistingEntryFolders()
    {
        WriteFlatMod();
        Assert.True(GameSetFolderConverter.Convert(Root).Success);

        _sets.Write("Mod/blocks.json", "[ { \"ID\": \"lamp\", \"Name\": \"Bright Lamp\", \"Textures\": { \"Active\": { \"All\": \"lamp\", \"Down\": \"shared\" } } } ]");
        var result = GameSetFolderConverter.Convert(Root);

        Assert.True(result.Success, string.Join("\n", result.Errors.Concat(result.Differences)));
        var lamp = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(Root, "Blocks", "lamp", "block.json")))!;
        Assert.Equal("Bright Lamp", lamp["Name"]!.GetValue<string>());
        Assert.Equal(new[] { "lamp", "stone" }, GameSetContentReader.ReadFolder(Root).Blocks.Select(b => b.Key));
    }

    [Fact]
    public void ExportFlat_ThenConvert_RoundTripsTheContent()
    {
        WriteFlatMod();
        Assert.True(GameSetFolderConverter.Convert(Root).Success);
        var folder = ContentSnapshot.Create(Root);

        GameSetFolderConverter.ExportFlatInPlace(Root);
        Assert.True(GameSetFolderConverter.HasFlatFiles(Root));
        Assert.Empty(ContentSnapshot.Differences(folder, ContentSnapshot.Create(Root)));

        Assert.True(GameSetFolderConverter.Convert(Root).Success);
        Assert.Empty(ContentSnapshot.Differences(folder, ContentSnapshot.Create(Root)));
    }

    [Fact]
    public void Convert_WithoutFlatFilesDoesNothing()
    {
        _sets.Config("Mod", "mod");
        _sets.Write("Mod/Blocks/stone/block.json", "{ \"ID\": \"stone\" }");

        var result = GameSetFolderConverter.Convert(Root);

        Assert.True(result.Success);
        Assert.Contains(result.Log, l => l.Contains("Nothing to convert"));
    }
}
