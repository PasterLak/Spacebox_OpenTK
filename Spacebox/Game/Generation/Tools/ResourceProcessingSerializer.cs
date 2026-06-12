using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using System.Collections.Generic;

namespace Spacebox.Game.Generation.Tools
{
    public class ResourceProcessingSerializer : IBlockSerializer
    {
        public string TagName => "processing_data";

        public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
        {
            var processingBlock = (ResourceProcessingBlock)block;

            if (processingBlock.InputStorage == null || processingBlock.OutputStorage == null || processingBlock.FuelStorage == null) return null;

            var inSlot = processingBlock.InputStorage.GetSlot(0, 0);
            var outSlot = processingBlock.OutputStorage.GetSlot(0, 0);
            var fuelSlot = processingBlock.FuelStorage.GetSlot(0, 0);

            if (!inSlot.HasItem && !outSlot.HasItem && !fuelSlot.HasItem && processingBlock.CurrentTick == 0) return null;

            var root = new CompoundTag(TagName);
            root.Add(new IntTag("flat_index", flatIndex));

            if (inSlot.HasItem)
            {
                if (itemsPalette.TryAdd(inSlot.Item.Id_string, indexInPalette)) indexInPalette++;
                root.Add(new IntTag("input_data", PackingTools.PackShorts(itemsPalette[inSlot.Item.Id_string], inSlot.Count)));
            }

            if (outSlot.HasItem)
            {
                if (itemsPalette.TryAdd(outSlot.Item.Id_string, indexInPalette)) indexInPalette++;
                root.Add(new IntTag("output_data", PackingTools.PackShorts(itemsPalette[outSlot.Item.Id_string], outSlot.Count)));
            }

            if (fuelSlot.HasItem)
            {
                if (itemsPalette.TryAdd(fuelSlot.Item.Id_string, indexInPalette)) indexInPalette++;
                root.Add(new IntTag("fuel_data", PackingTools.PackShorts(itemsPalette[fuelSlot.Item.Id_string], fuelSlot.Count)));
            }

            root.Add(new ShortTag("current_tick", processingBlock.CurrentTick));

            return root;
        }

        public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
        {
            var processingBlock = (ResourceProcessingBlock)block;

            if (tag.TryGetValue<IntTag>("input_data", out var inData))
            {
                PackingTools.UnpackShorts(inData.Value, out short itemIdx, out short count);
                var item = GameAssets.GetItemByFullID(paletteItems[itemIdx]);
                if (item != null) processingBlock.SetStorageAfterLoadFromNBT(item, (byte)count, processingBlock.InputStorage);
            }

            if (tag.TryGetValue<IntTag>("output_data", out var outData))
            {
                PackingTools.UnpackShorts(outData.Value, out short itemIdx, out short count);
                var item = GameAssets.GetItemByFullID(paletteItems[itemIdx]);
                if (item != null) processingBlock.SetStorageAfterLoadFromNBT(item, (byte)count, processingBlock.OutputStorage);
            }

            if (tag.TryGetValue<IntTag>("fuel_data", out var fuelData))
            {
                PackingTools.UnpackShorts(fuelData.Value, out short itemIdx, out short count);
                var item = GameAssets.GetItemByFullID(paletteItems[itemIdx]);
                if (item != null) processingBlock.SetStorageAfterLoadFromNBT(item, (byte)count, processingBlock.FuelStorage);
            }

            short ticks = 0;
            if (tag.TryGetValue<ShortTag>("current_tick", out var currentTickTag))
            {
                ticks = currentTickTag.Value;
                

            }

            if (ticks > 0)
            {
                processingBlock.RestoreTaskProgress(ticks);
            }
            else
            processingBlock.TryStart();
        }
    }
}