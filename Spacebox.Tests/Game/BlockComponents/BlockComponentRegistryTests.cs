using System.Text.Json;
using Spacebox.Editor.Data;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.Game.BlockComponents;

public class BlockComponentRegistryTests
{
    private static BlockJSON Json(string text) => JsonSerializer.Deserialize<BlockJSON>(text, ContentJson.CaseInsensitive);

    private static string[] Names(BlockComponentDefinition[] definitions)
    {
        var names = new string[definitions.Length];
        for (int i = 0; i < definitions.Length; i++) names[i] = definitions[i].Name;
        return names;
    }

    [Theory]
    [InlineData("furnace", "processing")]
    [InlineData("crusher", "processing")]
    [InlineData("generator", "generator")]
    public void LegacyTypes_ImplyTheirComponent(string type, string component)
    {
        var definitions = BlockComponentRegistry.Resolve(new BlockData("Machine", type), null);

        Assert.Equal(new[] { component }, Names(definitions));
    }

    [Fact]
    public void LegacyProcessing_UsesBlockTypeAsRecipeTypeAndProcessingPower()
    {
        var data = new BlockData("Furnace", "furnace") { ProcessingPower = 40, Efficiency = 2f };

        var processing = Assert.IsType<ProcessingComponentDefinition>(Assert.Single(BlockComponentRegistry.Resolve(data, null)));

        Assert.Equal("furnace", processing.RecipeType);
        Assert.Equal(40, processing.Power);
        Assert.Equal(2f, processing.Efficiency);
    }

    [Fact]
    public void Modular_CombinesListedComponentsInOrderWithoutDuplicates()
    {
        var json = Json("{\"Type\": \"modular\", \"Components\": [\"Processing\", \"storage\", \"processing\", \"light\", \"consumer\"], \"Processing\": {\"RecipeType\": \"Furnace\"}, \"StorageSize\": {\"X\": 4, \"Y\": 2}, \"Consumer\": {\"Power\": 35}}");
        var data = new BlockData("Smelter", "modular");

        var definitions = BlockComponentRegistry.Resolve(data, json);

        Assert.Equal(new[] { "processing", "storage", "light", "consumer" }, Names(definitions));
        Assert.Equal("furnace", ((ProcessingComponentDefinition)definitions[0]).RecipeType);
        Assert.Equal(new Vector2Byte(4, 2), ((StorageComponentDefinition)definitions[1]).Size);
        Assert.Equal(35, ((PowerConsumerComponentDefinition)definitions[3]).Power);
    }

    [Fact]
    public void LegacyType_CanAddComponentsByJson()
    {
        var json = Json("{\"Type\": \"furnace\", \"Components\": [\"storage\"]}");

        var definitions = BlockComponentRegistry.Resolve(new BlockData("Furnace", "furnace"), json);

        Assert.Equal(new[] { "processing", "storage" }, Names(definitions));
    }

    [Fact]
    public void UnknownComponents_AreSkipped()
    {
        var json = Json("{\"Type\": \"modular\", \"Components\": [\"teleporter\", \"light\", \"\"]}");

        var definitions = BlockComponentRegistry.Resolve(new BlockData("Lamp", "modular"), json);

        Assert.Equal(new[] { "light" }, Names(definitions));
    }

    [Fact]
    public void TypesWithoutComponentSupport_IgnoreComponents()
    {
        var json = Json("{\"Type\": \"block\", \"Components\": [\"light\"]}");

        Assert.Empty(BlockComponentRegistry.Resolve(new BlockData("Stone", "block"), json));
        Assert.False(BlockComponentRegistry.IsHostType("block"));
    }

    [Fact]
    public void OldJsonWithoutNewSections_KeepsDefaults()
    {
        var json = Json("{\"Type\": \"furnace\", \"Processing\": {\"Power\": 10}}");

        Assert.Empty(json.Components);
        Assert.Equal("", json.Processing.RecipeType);
        Assert.Equal(ConsumerJSON.DefaultPower, json.Consumer.Power);
    }

    [Fact]
    public void EveryHostTypeIsABlockFactoryType()
    {
        foreach (var type in BlockComponentRegistry.ComponentHostTypes)
            Assert.Contains(type, GameSetCatalog.BlockTypeNames, StringComparer.OrdinalIgnoreCase);
    }
}
