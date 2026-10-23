using SharpNBT;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Tests.Game.BlockComponents;

[Collection(GameAssetsCollection.Name)]
public class ComponentBlockSerializerTests
{
    private static ComponentBlock Furnace() => ComponentBlocks.Create(ComponentTestAssets.Data("furnace"), new BlockComponentDefinition[] { ComponentTestAssets.Processing() });

    [Fact]
    public void FurnaceBlocks_UseTheComponentSerializer()
    {
        Assert.IsType<ComponentBlockSerializer>(BlockSerializerRegistry.GetSerializer(Furnace().GetType()));
    }

    [Fact]
    public void OldProcessingTag_LoadsIntoTheProcessingComponent()
    {
        using var assets = new ComponentTestAssets();
        var tag = new CompoundTag("processing_data");
        tag.Add(new IntTag("flat_index", 7));
        tag.Add(new IntTag("input_data", PackingTools.PackShorts(0, 5)));
        tag.Add(new IntTag("output_data", PackingTools.PackShorts(1, 2)));
        tag.Add(new ShortTag("current_tick", 0));
        string[] palette = { assets.Ore.Id_string, assets.Ingot.Id_string };
        var block = Furnace();

        new ComponentBlockSerializer().Deserialize(block, tag, palette);

        block.TryGet<ProcessingComponent>(out var processing);
        Assert.Equal(5, processing.InputStorage.GetSlot(0, 0).Count);
        Assert.Equal(assets.Ore.Id, processing.InputStorage.GetSlot(0, 0).Item.Id);
        Assert.Equal(2, processing.OutputStorage.GetSlot(0, 0).Count);
        Assert.True(processing.IsRunning);
        processing.StopTask();
    }

    [Fact]
    public void EmptyComponentBlocks_SaveNothing()
    {
        var palette = new Dictionary<string, short>();
        short index = 0;

        Assert.Null(new ComponentBlockSerializer().Serialize(Furnace(), 3, palette, ref index));
        Assert.Empty(palette);
    }

    [Fact]
    public void ProcessingAndStorage_RoundTripInOneTag()
    {
        using var assets = new ComponentTestAssets();
        var definitions = new BlockComponentDefinition[] { new ProcessingComponentDefinition("comptest_none", 0, 1f), new StorageComponentDefinition(new Vector2Byte(2, 2), "Smelter") };
        var source = ComponentTestAssets.Modular(definitions);
        source.TryGet<ProcessingComponent>(out var sourceProcessing);
        source.TryGet<StorageComponent>(out var sourceStorage);
        sourceProcessing.FuelStorage.SetSlot(0, 0, assets.Rock, 9);
        sourceStorage.Storage.SetSlot(1, 1, assets.Ingot, 12);
        var palette = new Dictionary<string, short>();
        short index = 0;

        var tag = new ComponentBlockSerializer().Serialize(source, 42, palette, ref index);

        Assert.NotNull(tag);
        Assert.True(tag.TryGetValue<IntTag>("flat_index", out var flatIndex));
        Assert.Equal(42, flatIndex.Value);
        Assert.True(tag.TryGetValue<IntTag>("fuel_data", out _));
        Assert.True(tag.TryGetValue<LongArrayTag>(NBTKey.STORAGE.slots_data, out _));

        var names = new string[palette.Count];
        foreach (var pair in palette) names[pair.Value] = pair.Key;
        var target = ComponentTestAssets.Modular(definitions);
        new ComponentBlockSerializer().Deserialize(target, tag, names);

        target.TryGet<ProcessingComponent>(out var targetProcessing);
        target.TryGet<StorageComponent>(out var targetStorage);
        Assert.Equal(9, targetProcessing.FuelStorage.GetSlot(0, 0).Count);
        Assert.Equal(12, targetStorage.Storage.GetSlot(1, 1).Count);
        Assert.Equal(assets.Ingot.Id, targetStorage.Storage.GetSlot(1, 1).Item.Id);
    }
}
