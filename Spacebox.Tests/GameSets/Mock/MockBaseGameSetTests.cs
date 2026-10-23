using Spacebox.Game.Resource.Content;
using static Spacebox.Tests.GameSets.Mock.MockGameSets;

namespace Spacebox.Tests.GameSets.Mock;

public sealed class MockBaseGameSetTests
{
    [Fact]
    public void FolderBase_IsBuiltOnEmptyAndUsesEmptyAsItsOnlyFallback()
    {
        var result = Resolve("mock_base");

        Assert.True(result.Success, result.ErrorText);
        Assert.True(result.Stack.Base.IsBasedOnEmpty);
        Assert.False(result.Stack.Base.IsExtension);
        Assert.Equal(new[] { "empty" }, result.Stack.Fallbacks.Select(f => f.Id).ToArray());
        Assert.Empty(result.Stack.Extensions);
    }

    [Fact]
    public void FolderBase_ReadsBlocksInOrderItemsCategoriesBlueprintsAndRecipes()
    {
        var content = Read("mock_base");

        Assert.Equal(new[] { "mock_ore", "mock_stone" }, Keys(content.Blocks));
        Assert.Equal(new[] { "mock_drill", "mock_ingot" }, Keys(content.Items));
        Assert.Equal(new[] { "mock_parts" }, Keys(content.Categories));
        Assert.Equal(new[] { "mock_drill" }, Keys(content.Blueprints));
        Assert.Equal(new[] { "furnace|mock_ore" }, Keys(content.Recipes));
        Assert.All(content.Blocks, b => Assert.Null(b.Namespace));
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void FolderBase_EntryFilesFillInTheirOwnerAsProductAndIngredient()
    {
        var content = Read("mock_base");

        var blueprint = Assert.Single(content.Blueprints);
        var recipe = Assert.Single(content.Recipes);

        Assert.Equal("mock_drill", ReferencedItem(blueprint, "Product"));
        Assert.Equal(new[] { "mock_ingot" }, Ingredients(blueprint));
        Assert.Equal("mock_ore", ReferencedItem(recipe, "Ingredient"));
        Assert.Equal("mock_ingot", ReferencedItem(recipe, "Product"));
    }

    [Fact]
    public void FlatBase_ReadsTheOldFlatFiles()
    {
        var result = Resolve("mock_flat");
        Assert.True(result.Success, result.ErrorText);
        var content = result.Stack.ReadContent();

        Assert.True(result.Stack.Base.IsBasedOnEmpty);
        Assert.Equal(new[] { "flat_rock", "flat_crate" }, Keys(content.Blocks));
        Assert.Equal(new[] { "flat_gem" }, Keys(content.Items));
        Assert.Equal(new[] { "flat_tools" }, Keys(content.Categories));
        Assert.Equal(new[] { "flat_crate" }, Keys(content.Blueprints));
        Assert.Equal(new[] { "crusher|flat_rock" }, Keys(content.Recipes));
        Assert.Equal("crusher", (string)content.Recipes[0].Data["Type"]!);
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void FolderBase_IndexesItsTextures()
    {
        var stack = Resolve("mock_base").Stack;

        var assets = GameSetAssetIndex.Build(stack.AssetLayers);

        Assert.StartsWith(BaseFolder, assets.BlockTextures["mockStone"]);
    }

    [Fact]
    public void Bases_AreNotOfferedAsExtensions()
    {
        var result = Resolve("mock_base", "mock_flat");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("'mock_flat' is a GameSet built on Empty, not an extension"));
    }
}
