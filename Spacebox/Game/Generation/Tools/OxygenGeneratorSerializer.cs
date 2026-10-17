using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Tools;

public class OxygenGeneratorSerializer : IBlockSerializer
{
    public string TagName => "oxygen_generator_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var generator = (OxygenGeneratorBlock)block;
        var slot = generator.WaterStorage.GetSlot(0, 0);

        if (!slot.HasItem && generator.Water == 0 && generator.ConsumeProgress == 0f) return null;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new IntTag("water", generator.Water));
        root.Add(new FloatTag("consume_progress", generator.ConsumeProgress));

        if (slot.HasItem)
        {
            if (itemsPalette.TryAdd(slot.Item.Id_string, indexInPalette)) indexInPalette++;
            root.Add(new IntTag("slot_data", PackingTools.PackShorts(itemsPalette[slot.Item.Id_string], slot.Count)));
        }

        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var generator = (OxygenGeneratorBlock)block;

        int water = tag.TryGetValue<IntTag>("water", out var waterTag) ? waterTag.Value : 0;
        float progress = tag.TryGetValue<FloatTag>("consume_progress", out var progressTag) ? progressTag.Value : 0f;
        generator.Restore(water, progress);

        if (tag.TryGetValue<IntTag>("slot_data", out var slotData))
        {
            PackingTools.UnpackShorts(slotData.Value, out short itemIndex, out short count);
            if (itemIndex >= 0 && itemIndex < paletteItems.Length)
            {
                var item = GameAssets.GetItemByFullID(paletteItems[itemIndex]);
                if (item != null) generator.WaterStorage.SetSlot(0, 0, item, (byte)count);
            }
        }
    }
}
