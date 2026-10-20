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
            if (storageBlock.Storage == null) return null;

            bool hasName = storageBlock.NeedsToSaveName(out var name);
            if (!hasName && !storageBlock.Storage.HasAnyItems()) return null;

            var root = new CompoundTag(TagName);
            root.Add(new IntTag("flat_index", flatIndex));

            if (hasName)
            {
                root.Add(new StringTag(NBTKey.STORAGE.name, name));
            }

            root.Add(new LongArrayTag(NBTKey.STORAGE.slots_data, StorageSlotsNbt.Write(storageBlock.Storage, itemsPalette, ref indexInPalette)));
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
                StorageSlotsNbt.Read(newStorage, slotsData.ToArray(), paletteItems);
            }

            storageBlock.Storage = newStorage;
        }
    }
}