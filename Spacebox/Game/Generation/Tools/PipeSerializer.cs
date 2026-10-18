using SharpNBT;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public class PipeSerializer : IBlockSerializer
{
    public string TagName => "pipe_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var pipe = (PipeBlock)block;
        if (!pipe.Storage.HasAnyItems()) return null;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new IntTag("item", Pack(pipe.Buffer, itemsPalette, ref indexInPalette)));
        root.Add(new IntTag("filter", Pack(pipe.FilterSlot, itemsPalette, ref indexInPalette)));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var pipe = (PipeBlock)block;
        Unpack(tag, "item", pipe.Buffer, paletteItems);
        Unpack(tag, "filter", pipe.FilterSlot, paletteItems);
    }

    private static int Pack(ItemSlot slot, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        if (!slot.HasItem) return -1;

        if (itemsPalette.TryAdd(slot.Item.Id_string, indexInPalette)) indexInPalette++;
        return PackingTools.PackShorts(itemsPalette[slot.Item.Id_string], slot.Count);
    }

    private static void Unpack(CompoundTag tag, string key, ItemSlot slot, string[] paletteItems)
    {
        if (!tag.TryGetValue<IntTag>(key, out var data) || data.Value < 0) return;

        PackingTools.UnpackShorts(data.Value, out short itemIndex, out short count);
        if (itemIndex < 0 || itemIndex >= paletteItems.Length) return;

        var item = GameAssets.GetItemByFullID(paletteItems[itemIndex]);
        if (item != null) slot.SetData(item, (byte)count);
    }
}
