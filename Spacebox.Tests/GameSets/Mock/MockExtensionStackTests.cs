using Spacebox.Game.Resource.Content;
using static Spacebox.Tests.GameSets.Mock.MockGameSets;

namespace Spacebox.Tests.GameSets.Mock;

public sealed class MockExtensionStackTests
{
    [Fact]
    public void Addon_AddsNamespacedBlockItemBlueprintAndRecipe()
    {
        var content = Read("mock_base", "mock_addon");

        Assert.Contains(content.Blocks, b => b.Key == "mock_addon:mock_lamp" && b.Namespace == "mock_addon");
        Assert.Contains(content.Items, i => i.Key == "mock_addon:mock_bulb");
        Assert.Contains(content.Blocks, b => b.Key == "mock_stone");
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void Addon_ShortIdsInItsRecipesFindItsOwnItemsFirstThenTheBase()
    {
        var content = Read("mock_base", "mock_addon");

        var blueprint = Assert.Single(content.Blueprints, b => b.Key == "mock_addon:mock_lamp");
        var recipe = Assert.Single(content.Recipes, r => r.Key == "crusher|mock_stone");

        Assert.Equal("mock_addon:mock_lamp", ReferencedItem(blueprint, "Product"));
        Assert.Equal(new[] { "mock_addon:mock_bulb", "mock_ingot" }, Ingredients(blueprint));
        Assert.Equal("mock_stone", ReferencedItem(recipe, "Ingredient"));
        Assert.Equal("mock_addon:mock_bulb", ReferencedItem(recipe, "Product"));
    }

    [Fact]
    public void Patch_ChangesFieldsKeepsTheRestReplacesTheBlueprintAndRemovesAnEntry()
    {
        var content = Read("mock_base", "mock_patch");

        var stone = Assert.Single(content.Blocks, b => b.Key == "mock_stone");
        var drill = Assert.Single(content.Items, i => i.Key == "mock_drill");
        var blueprint = Assert.Single(content.Blueprints, b => b.Key == "mock_drill");

        Assert.Equal("Patched Stone", Name(stone));
        Assert.Equal(9, Int(stone, "Durability"));
        Assert.Equal(2, Int(stone, "Mass"));
        Assert.Null(stone.Namespace);
        Assert.Equal("Mock Drill", Name(drill));
        Assert.Equal("Cheaper drill", (string)drill.Data["Description"]!);
        Assert.Equal(1, blueprint.Data["Ingredients"]![0]!["Quantity"]!.GetValue<int>());
        Assert.Equal(new[] { "mock_stone" }, Keys(content.Blocks));
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void Patch_ReplacesTheBaseTextureWithTheSameName()
    {
        var stack = Resolve("mock_base", "mock_patch").Stack;

        var assets = GameSetAssetIndex.Build(stack.AssetLayers);

        Assert.Contains(Path.Combine("MockPatch", "Blocks"), assets.BlockTextures["mockStone"]);
    }

    [Theory]
    [InlineData(new[] { "mock_stack_a", "mock_stack_b" }, "Stone B")]
    [InlineData(new[] { "mock_stack_b", "mock_stack_a" }, "Stone A")]
    public void StackedExtensions_LoadInTheChosenOrderAndTheLastOneWins(string[] order, string expectedName)
    {
        var result = Resolve("mock_base", order);
        Assert.True(result.Success, result.ErrorText);
        var content = result.Stack.ReadContent();

        var stone = Assert.Single(content.Blocks, b => b.Key == "mock_stone");

        Assert.Equal(order, result.Stack.ExtensionIds);
        Assert.Equal(expectedName, Name(stone));
        Assert.Equal(3, Int(stone, "Mass"));
        Assert.Equal(5, Int(stone, "Durability"));
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void ExtensionPatchingAnotherExtensionsEntry_IsWarned()
    {
        var content = Read("mock_base", "mock_addon", "mock_stack_b");

        var lamp = Assert.Single(content.Blocks, b => b.Key == "mock_addon:mock_lamp");

        Assert.Equal("Lamp B", Name(lamp));
        Assert.Contains(content.Warnings, w => w.Contains("'mock_addon:mock_lamp' from extension 'mock_addon' is changed by extension 'mock_stack_b'"));
    }

    [Fact]
    public void ChildExtension_TurnsOnItsParentFirstAndUsesItsIds()
    {
        var result = Resolve("mock_base", "mock_child");
        Assert.True(result.Success, result.ErrorText);
        var content = result.Stack.ReadContent();

        var lamp = Assert.Single(content.Blocks, b => b.Key.EndsWith("mock_lamp"));
        var socket = Assert.Single(content.Blueprints, b => b.Key == "mock_child:mock_socket");

        Assert.Equal(new[] { "mock_addon", "mock_child" }, result.Stack.ExtensionIds);
        Assert.Equal("mock_addon:mock_lamp", lamp.Key);
        Assert.Equal("Bright Lamp", Name(lamp));
        Assert.Equal(new[] { "mock_addon:mock_bulb", "mock_ingot" }, Ingredients(socket));
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void ChildExtension_NamingItsParentByFolder_PatchesAndUsesTheParentIds()
    {
        var result = Resolve("mock_base", "mock_child_by_folder");
        Assert.True(result.Success, result.ErrorText);
        var content = result.Stack.ReadContent();

        var lamp = Assert.Single(content.Blocks, b => b.Key.EndsWith("mock_lamp"));
        var plug = Assert.Single(content.Blueprints, b => b.Key == "mock_child_by_folder:mock_plug");

        Assert.Equal(new[] { "mock_addon", "mock_child_by_folder" }, result.Stack.ExtensionIds);
        Assert.Equal("mock_addon:mock_lamp", lamp.Key);
        Assert.Equal("Folder Lamp", Name(lamp));
        Assert.Equal(new[] { "mock_addon:mock_bulb" }, Ingredients(plug));
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void ChildExtension_CanBeSelectedAloneAndFindsTheBaseThroughItsParent()
    {
        var result = Resolve("mock_child");

        Assert.True(result.Success, result.ErrorText);
        Assert.Equal("mock_base", result.Stack.Base.Id);
        Assert.Equal(new[] { "mock_addon", "mock_child" }, result.Stack.ExtensionIds);
    }

    [Theory]
    [InlineData("mock_base")]
    [InlineData("mock_flat")]
    public void FlatGeneralPack_WorksOnEveryBase(string baseId)
    {
        var content = Read(baseId, "mock_any");

        Assert.Contains(content.Blocks, b => b.Key == "mock_any:any_beacon");
        Assert.Contains(content.Items, i => i.Key == "mock_any:any_chip");
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void ExtensionForDefault_PatchesTheRealDefaultAndUsesItsItems()
    {
        var result = ResolveWithDefault("default", "mock_default_patch");
        Assert.True(result.Success, result.ErrorText);
        var content = result.Stack.ReadContent();

        var rock = Assert.Single(content.Blocks, b => b.Key == "light_rock");
        var alloy = Assert.Single(content.Blueprints, b => b.Key == "mock_default_patch:mock_alloy");

        Assert.Equal("Mock Light Rock", Name(rock));
        Assert.Equal("mineral", (string)rock.Data["Type"]!);
        Assert.Equal(new[] { "iron_ingot" }, Ingredients(alloy));
        Assert.Equal(DefaultGameSetContent.Content.Blocks.Count, content.Blocks.Count);
    }
}
