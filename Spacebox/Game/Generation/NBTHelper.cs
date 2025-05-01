using OpenTK.Mathematics;
using SharpNBT;
using Engine;
using System;
using Spacebox.Game.GUI;
using SharpCompress.Common;

namespace Spacebox.Game.Generation
{
    public static class NBTHelper
    {

        public static CompoundTag SectorOnlyToTag(Sector sector)
        {
            var root = new CompoundTag(sector.GetType().Name);


            root.Add(new IntTag("indexX", sector.PositionIndex.X));
            root.Add(new IntTag("indexY", sector.PositionIndex.Y));
            root.Add(new IntTag("indexZ", sector.PositionIndex.Z));

            return root;
        }

        public static void TagToSectorData(CompoundTag tag)
        {

        }

        public static CompoundTag SectorToTag(Sector sector)
        {
            var root = new CompoundTag(sector.GetType().Name);


            root.Add(new IntTag("indexX", sector.PositionIndex.X));
            root.Add(new IntTag("indexY", sector.PositionIndex.Y));
            root.Add(new IntTag("indexZ", sector.PositionIndex.Z));

            var entities = new ListTag("entities", TagType.Compound);

            var entitiesList = sector.Entities;

            for (int i = 0; i < entitiesList.Count; i++)
            {
                entities.Add(SpaceEntityToTag(entitiesList[i]));
            }

            root.Add(entities);

            return root;
        }


        public static SpaceEntity? TagToSpaceEntity(CompoundTag tag, Sector sector)
        {

            if (tag == null) return null;

            int id = tag.Get<IntTag>("id");
            string name = tag.Get<StringTag>("name");
            var x = tag.Get<FloatTag>("worldX");
            var y = tag.Get<FloatTag>("worldY");
            var z = tag.Get<FloatTag>("worldZ");

            SpaceEntity spaceEntity = new SpaceEntity(id, new Vector3(x, y, z), sector);
            spaceEntity.Name = name;

            var chunksList = tag.Get<ListTag>("chunks");

            var chunks = new List<Chunk>();

            foreach (CompoundTag chunk in chunksList)
            {
                chunks.Add(TagToChunk(chunk, spaceEntity));
            }
            spaceEntity.AddChunks(chunks.ToArray(), false);
            return spaceEntity;

        }

        public static List<CompoundTag> TagSpaceEntityToStorages(CompoundTag tag, out string[] paletteitemsString)
        {

            List<CompoundTag> list = new List<CompoundTag>();
            paletteitemsString = new string[0];
            if (tag == null) return list;

            var chunksList = tag.Get<ListTag>("chunks");

            foreach (CompoundTag chunk in chunksList)
            {



                if (chunk.TryGetValue<ListTag>("paletteItems", out var paletteItems))
                {

                    paletteitemsString = ListTagToPaletteItems(paletteItems);

                }
                if (chunk.TryGetValue<ListTag>("storages", out var storages))
                {

                    foreach (var storage in storages)
                    {

                        for (int i = 0; i < storages.Count; i++)
                        {
                            CompoundTag st = storages[i] as CompoundTag;
                            list.Add(st);
                        }

                        return list;
                    }
                }
            }

            return list;
        }

        public static Storage? TagSpaceEntityToStorage(CompoundTag tag, int positionInEntitySpace)
        {

            if (tag == null) return null;

            var storages = TagSpaceEntityToStorages(tag, out var paletteitemsString);

            foreach (var storage in storages)
            {


                var position = storage.Get<IntTag>("positionEntitySpace");

                if (position == positionInEntitySpace)
                {
                    var sizeXY = storage.Get<ShortTag>("sizeXY");

                    UnpackShortToBytes(sizeXY, out byte sizeX, out byte sizeY);

                    Storage newStorage = new Storage(sizeX, sizeY);

                    foreach (var slotData in storage.Get<LongArrayTag>("slotsData"))
                    {
                        UnpackShorts(slotData, out var paletteId, out var count, out var posX, out var posY);

                        var itemName = paletteitemsString[paletteId];

                        var slot = newStorage.GetSlot(posX, posY);

                        slot.SetData(GameAssets.GetItemByName(itemName), (byte)count);
                    }

                    return newStorage;
                }

            }



            return null;
        }

        public static CompoundTag SpaceEntityToTag(SpaceEntity entity)
        {
            var root = new CompoundTag(entity.GetType().Name);


            root.Add(new IntTag("id", entity.EntityID));
            root.Add(new StringTag("name", entity.Name));

            root.Add(new FloatTag("worldX", entity.PositionWorld.X));
            root.Add(new FloatTag("worldY", entity.PositionWorld.Y));
            root.Add(new FloatTag("worldZ", entity.PositionWorld.Z));

            var chunks = new ListTag("chunks", TagType.Compound);

            var chunksList = entity.Chunks;

            for (int i = 0; i < chunksList.Count; i++)
            {
                // if (chunksList[i].IsModified)
                chunks.Add(ChunkToTag(chunksList[i]));
            }

            root.Add(chunks);

            return root;
        }


        public static CompoundTag ChunkToTag(Chunk chunk)
        {
            var SIZE = Chunk.Size;
            var index = chunk.PositionIndex;
            var blocks = chunk.Blocks;

            var root = new CompoundTag(chunk.GetType().Name);

            root.Add(new ByteTag("indexX", SByteToByte(index.X)));
            root.Add(new ByteTag("indexY", SByteToByte(index.Y)));
            root.Add(new ByteTag("indexZ", SByteToByte(index.Z)));

            var listBlocks = new IntArrayTag("blocks", SIZE * SIZE * SIZE);

            short id = 0;
            Dictionary<short, short> palette = new Dictionary<short, short>();

            Dictionary<string, short> paletteItems = new Dictionary<string, short>();
            short indexInPalette = 0;

            List<int> resourceProcessingBlockData = new List<int>();

            List<int> blockWithDirIDs = new List<int>();

            List<CompoundTag> storagesList = new List<CompoundTag>();
            int indexIn1D = 0;
            for (byte x = 0; x < SIZE; x++)
            {
                for (byte y = 0; y < SIZE; y++)
                {
                    for (byte z = 0; z < SIZE; z++)
                    {
                        var block = blocks[x, y, z];

                        //var flatIndex = GetArrayIndex(x, y, z, SIZE);

                        if (palette.TryGetValue(block.BlockId, out var paletteKey))
                        {
                            listBlocks[indexIn1D] = paletteKey;
                        }
                        else
                        {
                            palette.Add(block.BlockId, id);
                            listBlocks[indexIn1D] = id;
                            id++;
                        }


                        int flatIndex = -1;

                        if (block.Direction != Direction.Up)
                        {
                            flatIndex = GetArrayIndex(x, y, z, SIZE);
                            blockWithDirIDs.Add(flatIndex); // indexIn1D
                            blockWithDirIDs.Add((byte)block.Direction);
                            //Debug.Log($"Direction save index: {indexIn1D} row direction {(byte)block.Direction} end pos {x},{y},{z} block id: {block.BlockId}");
                        }

                        if (block is ResourceProcessingBlock)
                        {
                            var blockRP = (ResourceProcessingBlock)block;

                            var storages = blockRP.GetAllStorages();

                            for (byte j = 0; j < storages.Length; j++)
                            {
                                AddItemsToPalette(storages[j], paletteItems, ref indexInPalette);
                            }

                            if (flatIndex == -1)
                                flatIndex = GetArrayIndex(x, y, z, SIZE);

                            if (ResourceProcessingBlockToTag(blockRP, flatIndex, paletteItems, out var result))
                            {
                                resourceProcessingBlockData.AddRange(result);
                            }
                        }

                        if (block is StorageBlock)
                        {
                            var blockRP = (StorageBlock)block;

                            var storage = blockRP.Storage;

                            if (storage != null)
                            {

                                AddItemsToPalette(storage, paletteItems, ref indexInPalette);


                                if (flatIndex == -1)
                                    flatIndex = GetArrayIndex(x, y, z, SIZE);

                                if (StorageBlockToTag(blockRP, paletteItems, out var result))
                                {
                                    storagesList.Add(result);
                                    //resourceProcessingBlockData.AddRange(result);
                                }
                            }


                        }

                        indexIn1D++;
                    }
                }
            }

            root.Add(listBlocks);

            var listPalette = new IntArrayTag("palette", palette.Count);

            foreach (var item in palette)
            {
                listPalette[item.Value] = item.Key;
            }

            root.Add(listPalette);

            var listPaletteitems = new ListTag("paletteItems", TagType.String);

            var paletteItemsArray = new string[paletteItems.Count];
            foreach (var item in paletteItems)
            {
                paletteItemsArray[item.Value] = item.Key;
            }

            for (int i = 0; i < paletteItemsArray.Length; i++)
            {
                listPaletteitems.Add(new StringTag(null, paletteItemsArray[i]));
            }

            root.Add(listPaletteitems);

            var listRotations = new IntArrayTag("rotations", blockWithDirIDs.Count);

            for (int x = 0; x < blockWithDirIDs.Count; x++)
            {
                listRotations[x] = blockWithDirIDs[x];
            }

            root.Add(listRotations);


            var listResourceProcessingBlocks = new IntArrayTag("resourceProcessingBlocks", resourceProcessingBlockData.Count);

            for (int x = 0; x < resourceProcessingBlockData.Count; x++)
            {
                listResourceProcessingBlocks[x] = resourceProcessingBlockData[x];
            }

            //Debug.Success("Saved resourceProcessingBlocks: " + resourceProcessingBlockData.Count );

            root.Add(listResourceProcessingBlocks);

            var storagesListTag = new ListTag("storages", TagType.Compound);

            storagesListTag.AddRange(storagesList);

            root.Add(storagesListTag);

            Debug.Log(root.PrettyPrinted());

            return root;
        }

        private static void AddAllBlockItemsToPalette(Dictionary<string, short> itemsPalette, List<ResourceProcessingBlock> blocks, ref short indexInPalette)
        {

            for (int i = 0; i < blocks.Count; i++)
            {
                var storages = blocks[i].GetAllStorages();

                for (byte j = 0; j < storages.Length; j++)
                {
                    AddItemsToPalette(storages[j], itemsPalette, ref indexInPalette);
                }
            }

        }

        private static void AddItemsToPalette(Storage storage, Dictionary<string, short> itemsPalette, ref short indexInPalette)
        {
            if (storage == null) return;

            if (storage.SlotsCount == 0) return;

            for (int x = 0; x < storage.SizeX; x++)
            {
                for (int y = 0; y < storage.SizeY; y++)
                {
                    var slot = storage.GetSlot(x, y);

                    if (!slot.HasItem) return;

                    if (itemsPalette.TryAdd(slot.Item.Name, indexInPalette))
                    {
                        indexInPalette++;
                    }

                }
            }
        }

        private static bool StorageSlotToTag(ItemSlot itemSlot, Dictionary<string, short> itemsPalette, out long data)
        {
            data = 0;
            if (itemSlot == null) return false;
            if (!itemSlot.HasItem) return false;

            data = PackShorts(itemsPalette[itemSlot.Item.Name], itemSlot.Count, itemSlot.Position.X, itemSlot.Position.Y);

            return true;
        }
        private static bool StorageBlockToTag(StorageBlock block, Dictionary<string, short> itemsPalette, out CompoundTag result)
        {
            result = null;
            if (block == null) return false;
            if (block.PositionIndex == -1) return false;

            var storage = block.Storage;
            if (storage == null) return false;

            var pos = 1;
            var storageSize = 4;

            var slotsData = new List<long>();


            short storageSizeXYPacked = PackBytesToShort(storage.SizeX, storage.SizeY);


            foreach (var slot in storage.GetAllSlots())
            {
                if (slot == null) continue;

                if (slot.HasItem)
                {
                    StorageSlotToTag(slot, itemsPalette, out var packedData);
                    slotsData.Add(packedData);
                }
            }

            //

            var root = new CompoundTag("storage");


            root.Add(new IntTag("positionEntitySpace", block.PositionIndex));
            root.Add(new ShortTag("sizeXY", storageSizeXYPacked));
            root.Add(new LongArrayTag("slotsData", slotsData));


            result = root;

            // Debug.Log(root.);
            return true;
        }
        private static bool ResourceProcessingBlockToTag(ResourceProcessingBlock block, int posIn1DArray, Dictionary<string, short> itemsPalette, out int[] result) // in out fuel
        {
            result = new int[4] { posIn1DArray, 0, 0, 0 };

            if (block == null) return false;

            if (block.InputStorage == null) return false;
            if (block.OutputStorage == null) return false;
            if (block.FuelStorage == null) return false;

            var inSlot = block.InputStorage.GetSlot(0, 0);
            var outSlot = block.OutputStorage.GetSlot(0, 0);
            var fuelSlot = block.FuelStorage.GetSlot(0, 0);

            if (!inSlot.HasItem && !outSlot.HasItem && !fuelSlot.HasItem) return false;

            int inputData = 0;
            int outputData = 0;
            int fuelData = 0;

            if (!inSlot.HasItem) inputData = 0;
            else
            {
                inputData = PackShorts(itemsPalette[inSlot.Item.Name], inSlot.Count);
            }
            if (!outSlot.HasItem) outputData = 0;
            else
            {
                outputData = PackShorts(itemsPalette[outSlot.Item.Name], outSlot.Count);
            }
            if (!fuelSlot.HasItem) fuelData = 0;
            else
            {
                fuelData = PackShorts(itemsPalette[fuelSlot.Item.Name], fuelSlot.Count);
            }



            result[1] = inputData;
            result[2] = outputData;
            result[3] = fuelData;

            return true;
        }

        public static int PackShorts(short a, short b)
        {
            return (((int)(ushort)a) << 16) | ((ushort)b);
        }

        public static void UnpackShorts(int packed, out short a, out short b)
        {
            a = (short)((packed >> 16) & 0xFFFF);
            b = (short)(packed & 0xFFFF);
        }

        public static short PackBytesToShort(byte high, byte low)
        {
            return (short)((high << 8) | low);
        }

        public static void UnpackShortToBytes(short packed, out byte high, out byte low)
        {
            high = (byte)((packed >> 8) & 0xFF);
            low = (byte)(packed & 0xFF);
        }
        public static long PackInts(int high, int low)
        {
            return ((long)(uint)high << 32) | (uint)low;
        }

        public static void UnpackInts(long packed, out int high, out int low)
        {
            high = (int)(packed >> 32);
            low = (int)(packed & 0xFFFFFFFF);
        }
        public static long PackShorts(short a, short b, short c, short d)
        {
            return ((long)(ushort)a << 48) |
                   ((long)(ushort)b << 32) |
                   ((long)(ushort)c << 16) |
                   (ushort)d;
        }

        public static void UnpackShorts(long packed, out short a, out short b, out short c, out short d)
        {
            a = (short)((packed >> 48) & 0xFFFF);
            b = (short)((packed >> 32) & 0xFFFF);
            c = (short)((packed >> 16) & 0xFFFF);
            d = (short)(packed & 0xFFFF);
        }


        public static void TagToStorageBlock(int dataIn, int dataOut, int dataFuel, ResourceProcessingBlock block, string[] paletteitems)
        {
            if (dataIn + dataOut + dataFuel == 0) return;

            UnpackShorts(dataIn, out short inItem, out short inCount);
            UnpackShorts(dataOut, out short outItem, out short outCount);
            UnpackShorts(dataFuel, out short fuelItem, out short fuelCount);

            if (inCount > 0)
            {
                var inItm = GameAssets.GetItemByName(paletteitems[inItem]);

                if (inItm != null)
                {
                    block.SetStorageAfterLoadFromNBT(inItm, (byte)inCount, block.InputStorage);
                }
            }

            if (outCount > 0)
            {
                var outItm = GameAssets.GetItemByName(paletteitems[outItem]);

                if (outItm != null)
                {
                    block.SetStorageAfterLoadFromNBT(outItm, (byte)outCount, block.OutputStorage);
                }
            }
            if (fuelCount > 0)
            {
                var fuelItm = GameAssets.GetItemByName(paletteitems[fuelItem]);

                if (fuelItm != null)
                {
                    block.SetStorageAfterLoadFromNBT(fuelItm, (byte)fuelCount, block.FuelStorage);
                }
            }

            block.TryStart();

        }


        public static void TagToResourceProcessingBlock(int dataIn, int dataOut, int dataFuel, ResourceProcessingBlock block, string[] paletteitems)
        {
            if (dataIn + dataOut + dataFuel == 0) return;

            UnpackShorts(dataIn, out short inItem, out short inCount);
            UnpackShorts(dataOut, out short outItem, out short outCount);
            UnpackShorts(dataFuel, out short fuelItem, out short fuelCount);

            if (inCount > 0)
            {
                var inItm = GameAssets.GetItemByName(paletteitems[inItem]);

                if (inItm != null)
                {
                    block.SetStorageAfterLoadFromNBT(inItm, (byte)inCount, block.InputStorage);
                }
            }

            if (outCount > 0)
            {
                var outItm = GameAssets.GetItemByName(paletteitems[outItem]);

                if (outItm != null)
                {
                    block.SetStorageAfterLoadFromNBT(outItm, (byte)outCount, block.OutputStorage);
                }
            }
            if (fuelCount > 0)
            {
                var fuelItm = GameAssets.GetItemByName(paletteitems[fuelItem]);

                if (fuelItm != null)
                {
                    block.SetStorageAfterLoadFromNBT(fuelItm, (byte)fuelCount, block.FuelStorage);
                }
            }

            block.TryStart();

        }

        private static string[] ListTagToPaletteItems(ListTag tag)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < tag.Count; i++)
            {
                StringTag st = tag[i] as StringTag;
                list.Add(st.Value);
            }

            return list.ToArray();
        }


        public static Chunk? TagToChunk(CompoundTag tag, SpaceEntity spaceEntity)
        {
            //Debug.Log(tag.PrettyPrinted());
            const byte SIZE = Chunk.Size;
            sbyte ix = ByteToSByte(tag.Get<ByteTag>("indexX").Value);
            sbyte iy = ByteToSByte(tag.Get<ByteTag>("indexY").Value);
            sbyte iz = ByteToSByte(tag.Get<ByteTag>("indexZ").Value);

            var listBlocks = tag.Get<IntArrayTag>("blocks").ToArray();
            var listPalette = tag.Get<IntArrayTag>("palette").ToArray();
            var listRotations = tag.Get<IntArrayTag>("rotations").ToArray();





            short[] ids = new short[SIZE * SIZE * SIZE];

            if (ids.Length != listBlocks.Length)
            {
                Debug.Error("NBTHelper - TagToChunk, listBlocks has wrong length!");
                return null;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = (short)listPalette[listBlocks[i]];
            }


            Block[,,] blocks = ReconstructBlocksFrom1D(ids);

            for (int i = 0; i < listRotations.Length; i += 2)
            {
                int index1D = listRotations[i];
                byte direction = (byte)listRotations[i + 1];

                GetCoordinatesFrom1DIndex(index1D, SIZE, out byte x, out byte y, out byte z);

                if (x < SIZE && y < SIZE && z < SIZE)
                {
                    blocks[x, y, z].Direction = (Direction)direction;
                }

            }


            if (tag.TryGetValue<ListTag>("paletteItems", out var paletteItems))
            {
                var paletteitemsString = ListTagToPaletteItems(paletteItems);

                if (tag.TryGetValue<IntArrayTag>("resourceProcessingBlocks", out var resourcePBData))
                {
                    if (resourcePBData.Count % 4 == 0 && paletteitemsString.Length > 0)
                    {
                        for (int i = 0; i < resourcePBData.Count; i += 4)
                        {
                            GetCoordinatesFrom1DIndex(resourcePBData[i], SIZE, out byte x, out byte y, out byte z);

                            var block = blocks[x, y, z] as ResourceProcessingBlock;

                            if (block == null)
                            {
                                continue;
                            }

                            TagToResourceProcessingBlock(resourcePBData[i + 1], resourcePBData[i + 2], resourcePBData[i + 3], block, paletteitemsString);
                        }
                    }
                }

                if (tag.TryGetValue<ListTag>("storages", out var storages))
                {
                }

            }




            Chunk chunk = new Chunk(new Vector3SByte(ix, iy, iz), spaceEntity, blocks, true, false);

            return chunk;
        }

        public static byte SByteToByte(sbyte value)
        {
            return unchecked((byte)value);
        }
        public static sbyte ByteToSByte(byte value)
        {
            return unchecked((sbyte)value);
        }

        private static void GetCoordinatesFrom1DIndex(int index, int size, out byte x, out byte y, out byte z)
        {
            x = (byte)(index % size);
            y = (byte)((index / size) % size);
            z = (byte)(index / (size * size));
        }

        private static Block[,,] ReconstructBlocksFrom1D(short[] ids)
        {
            if (ids.Length != Chunk.Size * Chunk.Size * Chunk.Size)
            {
                throw new ArgumentException("Invalid number of Block IDs for chunk reconstruction.");
            }
            Block[,,] blocks = new Block[Chunk.Size, Chunk.Size, Chunk.Size];
            int index = 0;
            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.Size; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        blocks[x, y, z] = GameAssets.CreateBlockFromId(ids[index]);
                        index++;
                    }
            return blocks;
        }


        public static int GetArrayIndex(byte x, byte y, byte z, byte chunkSize)
        {
            return (x + y * chunkSize + z * chunkSize * chunkSize);
        }
    }
}
