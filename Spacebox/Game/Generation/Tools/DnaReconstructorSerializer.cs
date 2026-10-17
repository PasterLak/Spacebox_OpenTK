using SharpNBT;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public class DnaReconstructorSerializer : IBlockSerializer
{
    public string TagName => "dna_reconstructor_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var reconstructor = (DnaReconstructorBlock)block;
        var slot = reconstructor.DnaStorage.GetSlot(0, 0);
        if (!slot.HasItem) return null;

        if (itemsPalette.TryAdd(slot.Item.Id_string, indexInPalette)) indexInPalette++;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new IntTag("slot_data", PackingTools.PackShorts(itemsPalette[slot.Item.Id_string], slot.Count)));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var reconstructor = (DnaReconstructorBlock)block;
        if (!tag.TryGetValue<IntTag>("slot_data", out var slotData)) return;

        PackingTools.UnpackShorts(slotData.Value, out short itemIndex, out short count);
        if (itemIndex < 0 || itemIndex >= paletteItems.Length) return;

        var item = GameAssets.GetItemByFullID(paletteItems[itemIndex]);
        if (item != null) reconstructor.DnaStorage.SetSlot(0, 0, item, (byte)count);
    }
}
