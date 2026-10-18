using SharpNBT;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public class DisassemblerSerializer : IBlockSerializer
{
    public string TagName => "disassembler_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var disassembler = (DisassemblerBlock)block;
        if (!disassembler.InputStorage.HasAnyItems() && !disassembler.OutputStorage.HasAnyItems() && disassembler.Progress == 0) return null;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new LongArrayTag("input_slots", StorageSlotsNbt.Write(disassembler.InputStorage, itemsPalette, ref indexInPalette)));
        root.Add(new LongArrayTag("output_slots", StorageSlotsNbt.Write(disassembler.OutputStorage, itemsPalette, ref indexInPalette)));
        root.Add(new IntTag("progress", disassembler.Progress));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var disassembler = (DisassemblerBlock)block;

        if (tag.TryGetValue<LongArrayTag>("input_slots", out var inputSlots))
            StorageSlotsNbt.Read(disassembler.InputStorage, inputSlots.ToArray(), paletteItems);

        if (tag.TryGetValue<LongArrayTag>("output_slots", out var outputSlots))
            StorageSlotsNbt.Read(disassembler.OutputStorage, outputSlots.ToArray(), paletteItems);

        ReadLegacySlot(tag, "input_data", disassembler.InputStorage, paletteItems);
        ReadLegacySlot(tag, "output_data", disassembler.OutputStorage, paletteItems);

        if (tag.TryGetValue<IntTag>("progress", out var progress))
            disassembler.RestoreProgress(progress.Value);
    }

    private static void ReadLegacySlot(CompoundTag tag, string key, Storage storage, string[] paletteItems)
    {
        if (!tag.TryGetValue<IntTag>(key, out var data)) return;

        PackingTools.UnpackShorts(data.Value, out short itemIndex, out short count);
        if (itemIndex < 0 || itemIndex >= paletteItems.Length) return;

        var item = GameAssets.GetItemByFullID(paletteItems[itemIndex]);
        if (item != null) storage.GetSlot(0, 0).SetData(item, (byte)count);
    }
}
