using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using System.Collections.Generic;
using Engine;

namespace Spacebox.Game.Generation.Tools
{
    public class StorageBlockSerializer : IBlockSerializer
    {
        public string TagName => "storage_data";

        public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
        {
            var storageBlock = (StorageBlock)block;
            if (storageBlock.Storage == null || !storageBlock.Storage.HasAnyItems()) return null;

            var root = new CompoundTag(TagName);
            root.Add(new IntTag("flat_index", flatIndex));

            if (storageBlock.NeedsToSaveName(out var name))
            {
                root.Add(new StringTag(NBTKey.STORAGE.name, name));
            }

            var slotsData = new List<long>();
            foreach (var slot in storageBlock.Storage.GetAllSlots())
            {
                if (slot != null && slot.HasItem)
                {
                    if (itemsPalette.TryAdd(slot.Item.Id_string, indexInPalette))
                    {
                        indexInPalette++;
                    }

                    long packedData = PackingTools.PackShorts(itemsPalette[slot.Item.Id_string], slot.Count, slot.Position.X, slot.Position.Y);
                    slotsData.Add(packedData);
                }
            }

            root.Add(new LongArrayTag(NBTKey.STORAGE.slots_data, slotsData.ToArray()));
            return root;
        }

        public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
        {
            var storageBlock = (StorageBlock)block;

            var storageBlockData = GameAssets.GetBlockDataById(storageBlock.Id) as StorageBlockData;
            var size = new Vector2Byte(8, 3);
            if (storageBlockData != null)
            {
                size = storageBlockData.Size;
            }

            Storage newStorage = new Storage(size.X, size.Y);

            if (tag.TryGetValue<StringTag>(NBTKey.STORAGE.name, out var nameTag))
            {
                newStorage.Name = nameTag.Value;
            }

            if (tag.TryGetValue<LongArrayTag>(NBTKey.STORAGE.slots_data, out var slotsData))
            {
                var arr = slotsData.ToArray();
                foreach (var slotData in arr)
                {
                    PackingTools.UnpackShorts(slotData, out var paletteId, out var count, out var posX, out var posY);
                    var itemIdStr = paletteItems[paletteId];
                    var item = GameAssets.GetItemByFullID(itemIdStr);

                    if (item != null)
                    {
                        newStorage.GetSlot(posX, posY)?.SetData(item, (byte)count);
                    }
                }
            }

            storageBlock.Storage = newStorage;
        }
    }
}