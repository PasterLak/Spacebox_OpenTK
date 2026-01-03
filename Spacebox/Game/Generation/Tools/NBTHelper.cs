using Engine;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Resource;
using System.Collections.Generic;


namespace Spacebox.Game.Generation.Tools;

public static class NBTHelper
{

    public static CompoundTag SectorOnlyToTag(Sector sector)
    {
        var root = new CompoundTag(sector.GetType().Name);


        root.Add(new IntTag(NBTKey.SECTOR.index_x, sector.PositionIndex.X));
        root.Add(new IntTag(NBTKey.SECTOR.index_y, sector.PositionIndex.Y));
        root.Add(new IntTag(NBTKey.SECTOR.index_z, sector.PositionIndex.Z));

        long[] data = new long[sector.SuppressedEntities.Count];

        int i = 0;
        foreach (var id in sector.SuppressedEntities)
        {
            data[i++] = id;
        }

        root.Add(new LongArrayTag(NBTKey.SECTOR.suppressed_ids, data));

        return root;
    }

    public static void TagToSectorOnly(CompoundTag tag, Sector sector)
    {
        if (tag == null) return;

        if (tag.TryGetValue<LongArrayTag>(NBTKey.SECTOR.suppressed_ids, out var arrayTag))
        {
            sector.SuppressedEntities.Clear();

            foreach (long packedId in arrayTag)
            {

                sector.SuppressedEntities.Add(packedId);
            }
        }

    }

    public static SpaceEntity? TagToSpaceEntity(CompoundTag tag, Sector sector)
    {

        if (tag == null) return null;

        long id = tag.Get<LongTag>(NBTKey.ENTITY.id);

        string file_name = tag.Get<StringTag>(NBTKey.ENTITY.name);

        var x = tag.Get<FloatTag>(NBTKey.ENTITY.local_x);
        var y = tag.Get<FloatTag>(NBTKey.ENTITY.local_y);
        var z = tag.Get<FloatTag>(NBTKey.ENTITY.local_z);


        var worldPos = sector.LocalToWorldPosition(new Vector3(x, y, z));

        SpaceEntity spaceEntity;


        if (id >= 0)
        {
            var stubData = new NotGeneratedEntity
            {
                Id = id,
                positionWorld = worldPos
            };

            var asteroid = new Asteroid(stubData, sector, false);

            asteroid.IsGenerated = true;
            spaceEntity = asteroid;
        }
        else
        {
            spaceEntity = new SpaceEntity(id, worldPos, sector);
        }

        spaceEntity.Name = file_name;
        spaceEntity.IsGenerated = true;

        if (tag.ContainsKey(NBTKey.ENTITY.rotation_x))
        {
            var rx = tag.Get<FloatTag>(NBTKey.ENTITY.rotation_x);
            var ry = tag.Get<FloatTag>(NBTKey.ENTITY.rotation_y);
            var rz = tag.Get<FloatTag>(NBTKey.ENTITY.rotation_z);
            var rw = tag.Get<FloatTag>(NBTKey.ENTITY.rotation_w);

            spaceEntity.Rotation = new Quaternion(rx, ry, rz, rw).ToEulerAngles();
        }
        var chunksList = tag.Get<ListTag>(NBTKey.ENTITY.chunks);

        var chunks = new List<Chunk>();

        foreach (CompoundTag chunk in chunksList)
        {
            chunks.Add(TagToChunk(chunk, spaceEntity));
        }
        spaceEntity.AddChunks(chunks.ToArray(), false);
        return spaceEntity;

    }


    public static CompoundTag SpaceEntityToTag(SpaceEntity entity)
    {
        var root = new CompoundTag(entity.GetType().Name);


        root.Add(new LongTag(NBTKey.ENTITY.id, entity.EntityID));
        root.Add(new StringTag(NBTKey.ENTITY.name, entity.Name));

        var localPos = entity.Sector.WorldToLocalPosition(entity.PositionWorld);

        root.Add(new FloatTag(NBTKey.ENTITY.local_x, localPos.X));
        root.Add(new FloatTag(NBTKey.ENTITY.local_y, localPos.Y));
        root.Add(new FloatTag(NBTKey.ENTITY.local_z, localPos.Z));

        Quaternion rot = Quaternion.FromEulerAngles(entity.Rotation);

        root.Add(new FloatTag(NBTKey.ENTITY.rotation_x, rot.X));
        root.Add(new FloatTag(NBTKey.ENTITY.rotation_y, rot.Y));
        root.Add(new FloatTag(NBTKey.ENTITY.rotation_z, rot.Z));
        root.Add(new FloatTag(NBTKey.ENTITY.rotation_w, rot.W));

        var chunks = new ListTag(NBTKey.ENTITY.chunks, TagType.Compound);

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

        root.Add(new ByteTag(NBTKey.CHUNK.index_x, PackingTools.SByteToByte(index.X)));
        root.Add(new ByteTag(NBTKey.CHUNK.index_y, PackingTools.SByteToByte(index.Y)));
        root.Add(new ByteTag(NBTKey.CHUNK.index_z, PackingTools.SByteToByte(index.Z)));

        var listBlocks = new IntArrayTag(NBTKey.CHUNK.blocks, SIZE * SIZE * SIZE);

        short id = 0;
        Dictionary<string, short> paletteBlocks = new Dictionary<string, short>();
   

        Dictionary<string, short> paletteItems = new Dictionary<string, short>();
        short indexInPalette = 0;

        List<int> resourceProcessingBlockData = new List<int>();

        List<int> blockWithDirectionIDs = new List<int>();
        List<int> blockWithRotationIDs = new List<int>();

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

                    var blockFullId = GameAssets.GetBlockFullId(block);

                    if (paletteBlocks.TryGetValue(blockFullId, out var paletteKey))
                    {
                        listBlocks[indexIn1D] = paletteKey;
                    }
                    else
                    {
                        paletteBlocks.Add(blockFullId, id);
                        listBlocks[indexIn1D] = id;
                        id++;
                    }


                    int flatIndex = -1;

                    if (block.Direction != Direction.Up)
                    {
                        flatIndex = GetArrayIndex(x, y, z, SIZE);
                        blockWithDirectionIDs.Add(flatIndex); // indexIn1D
                        blockWithDirectionIDs.Add((byte)block.Direction);
                        //Debug.Log($"Direction save index: {indexIn1D} row direction {(byte)block.Direction} end pos {x},{y},{z} block id: {block.BlockId}");
                    }
                    if (block.Rotation != Rotation.None)
                    {
                        if (flatIndex == -1)
                            flatIndex = GetArrayIndex(x, y, z, SIZE);
                        blockWithRotationIDs.Add(flatIndex); // indexIn1D
                        blockWithRotationIDs.Add((byte)block.Rotation);
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

                        if (storage != null && storage.HasAnyItems())
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

        var listPaletteBlocks = new ListTag(NBTKey.CHUNK.palette_blocks, TagType.String);

        var paletteBlocksArray = new string[paletteBlocks.Count];
        foreach (var block in paletteBlocks)
        {
            paletteBlocksArray[block.Value] = block.Key;
        }

        for (int i = 0; i < paletteBlocksArray.Length; i++)
        {
            listPaletteBlocks.Add(new StringTag(null, paletteBlocksArray[i]));
        }

        root.Add(listPaletteBlocks);

        var listPaletteItems = new ListTag(NBTKey.CHUNK.palette_items, TagType.String);

        var paletteItemsArray = new string[paletteItems.Count];

        foreach (var item in paletteItems)
        {
            paletteItemsArray[item.Value] = item.Key;
        }

        for (int i = 0; i < paletteItemsArray.Length; i++)
        {
            listPaletteItems.Add(new StringTag(null, paletteItemsArray[i]));
        }

        root.Add(listPaletteItems);

        var listDirections = new IntArrayTag(NBTKey.CHUNK.directions, blockWithDirectionIDs.Count);

        for (int x = 0; x < blockWithDirectionIDs.Count; x++)
        {
            listDirections[x] = blockWithDirectionIDs[x];
        }

        root.Add(listDirections);

        var listRotations = new IntArrayTag(NBTKey.CHUNK.rotations, blockWithRotationIDs.Count);

        for (int x = 0; x < blockWithRotationIDs.Count; x++)
        {
            listRotations[x] = blockWithRotationIDs[x];
        }

        root.Add(listRotations);


        var listResourceProcessingBlocks = new IntArrayTag(NBTKey.CHUNK.processing_blocks, resourceProcessingBlockData.Count);

        for (int x = 0; x < resourceProcessingBlockData.Count; x++)
        {
            listResourceProcessingBlocks[x] = resourceProcessingBlockData[x];
        }

        //Debug.Success("Saved resourceProcessingBlocks: " + resourceProcessingBlockData.Count );

        root.Add(listResourceProcessingBlocks);

        var storagesListTag = new ListTag(NBTKey.CHUNK.storages, TagType.Compound);

        storagesListTag.AddRange(storagesList);
        root.Add(storagesListTag);
       
        //Debug.Log(root.PrettyPrinted());

        return root;
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

                if (slot.HasItem && itemsPalette.TryAdd(slot.Item.Id_string, indexInPalette))
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

        data = PackingTools.PackShorts(itemsPalette[itemSlot.Item.Id_string], itemSlot.Count, itemSlot.Position.X, itemSlot.Position.Y);

        return true;
    }
    private static bool StorageBlockToTag(StorageBlock block, Dictionary<string, short> itemsPalette, out CompoundTag result)
    {
        result = null;
        if (block == null) return false;
        //if (block.PositionIndex == -1) return false;

        var storage = block.Storage;
        if (storage == null) return false;

        var pos = 1;
        var storageSize = 4;


        var slotsData = new List<long>();


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

        var root = new CompoundTag(NBTKey.STORAGE.storage);


        root.Add(new ShortTag(NBTKey.STORAGE.position_chunk, block.PositionIndex));

        if (block.NeedsToSaveName(out var name))
        {
            root.Add(new StringTag(NBTKey.STORAGE.name, name));
        }

        root.Add(new LongArrayTag(NBTKey.STORAGE.slots_data, slotsData));


        result = root;

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
            inputData = PackingTools.PackShorts(itemsPalette[inSlot.Item.Id_string], inSlot.Count);
        }
        if (!outSlot.HasItem) outputData = 0;
        else
        {
            outputData = PackingTools.PackShorts(itemsPalette[outSlot.Item.Id_string], outSlot.Count);
        }
        if (!fuelSlot.HasItem) fuelData = 0;
        else
        {
            fuelData = PackingTools.PackShorts(itemsPalette[fuelSlot.Item.Id_string], fuelSlot.Count);
        }



        result[1] = inputData;
        result[2] = outputData;
        result[3] = fuelData;

        return true;
    }

    public static void TagToResourceProcessingBlock(int dataIn, int dataOut, int dataFuel, ResourceProcessingBlock block, string[] paletteitems)
    {
        if (dataIn + dataOut + dataFuel == 0) return;

        PackingTools.UnpackShorts(dataIn, out short inItem, out short inCount);
        PackingTools.UnpackShorts(dataOut, out short outItem, out short outCount);
        PackingTools.UnpackShorts(dataFuel, out short fuelItem, out short fuelCount);

        if (inCount > 0)
        {
            var inItm = GameAssets.GetItemByFullID(paletteitems[inItem]);

            if (inItm != null)
            {
                block.SetStorageAfterLoadFromNBT(inItm, (byte)inCount, block.InputStorage);
            }
        }

        if (outCount > 0)
        {
            var outItm = GameAssets.GetItemByFullID(paletteitems[outItem]);

            if (outItm != null)
            {
                block.SetStorageAfterLoadFromNBT(outItm, (byte)outCount, block.OutputStorage);
            }
        }
        if (fuelCount > 0)
        {
            var fuelItm = GameAssets.GetItemByFullID(paletteitems[fuelItem]);

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

        const byte SIZE = Chunk.Size;
        sbyte ix = PackingTools.ByteToSByte(tag.Get<ByteTag>(NBTKey.CHUNK.index_x).Value);
        sbyte iy = PackingTools.ByteToSByte(tag.Get<ByteTag>(NBTKey.CHUNK.index_y).Value);
        sbyte iz = PackingTools.ByteToSByte(tag.Get<ByteTag>(NBTKey.CHUNK.index_z).Value);

        var listBlocks = tag.Get<IntArrayTag>(NBTKey.CHUNK.blocks).ToArray();
        var listPaletteBlocks = tag.Get<ListTag>(NBTKey.CHUNK.palette_blocks).ToArray();

        var listDirections = new int[0];
        if (tag.TryGetValue<IntArrayTag>(NBTKey.CHUNK.directions, out var list)){
            listDirections = list;
        }
        var listRotations = new int[0];
        if (tag.TryGetValue<IntArrayTag>(NBTKey.CHUNK.rotations, out var list2))
        {
            listRotations = list2;
        }



        short[] ids = new short[SIZE * SIZE * SIZE];

        if (ids.Length != listBlocks.Length)
        {
            Debug.Error("NBTHelper - TagToChunk, listBlocks has wrong length!");
            return null;
        }

        for (int i = 0; i < ids.Length; i++)
        {
            var stringId = listPaletteBlocks[listBlocks[i]] as StringTag;

            var blockData = GameAssets.GetBlockByFullID(stringId.Value);

            if (blockData != null)
            {
                ids[i] = GameAssets.GetBlockByFullID(stringId.Value).Id;
            }
            else
            {
                Debug.Error($"[NBTHelper] - TagToChunk: BlockData is null for block id string: {stringId.Value} at index {i} in chunk {ix},{iy},{iz}. Using 'void' block instead.");
                ids[i] = 0;   // check null   TODO
            }

        }

        Block[,,] blocks = ReconstructBlocksFrom1D(ids);

        for (int i = 0; i < listDirections.Length; i += 2)
        {
            int index1D = listDirections[i];
            byte direction = (byte)listDirections[i + 1];

            GetCoordinatesFrom1DIndex(index1D, SIZE, out byte x, out byte y, out byte z);

            if (x < SIZE && y < SIZE && z < SIZE)
            {
                blocks[x, y, z].Direction = (Direction)direction;
            }

        }
        for (int i = 0; i < listRotations.Length; i += 2)
        {
            int index1D = listRotations[i];
            byte rot = (byte)listRotations[i + 1];

            GetCoordinatesFrom1DIndex(index1D, SIZE, out byte x, out byte y, out byte z);

            if (x < SIZE && y < SIZE && z < SIZE)
            {
                blocks[x, y, z].Rotation = (Rotation)rot;
            }

        }


        if (tag.TryGetValue<ListTag>(NBTKey.CHUNK.palette_items, out var paletteItems))
        {
            var paletteitemsString = ListTagToPaletteItems(paletteItems);

            if (tag.TryGetValue<IntArrayTag>(NBTKey.CHUNK.processing_blocks, out var resourcePBData))
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

            if (tag.TryGetValue<ListTag>(NBTKey.CHUNK.storages, out var storages))
            {

                foreach (CompoundTag storage in storages)
                {


                    if (storage.TryGetValue<ShortTag>(NBTKey.STORAGE.position_chunk, out var posTag))
                    {
                        Vector3Byte pos = StorageBlock.PositionIndexToPositionInChunk((ushort)posTag.Value);

                        var blockId = blocks[pos.X, pos.Y, pos.Z].Id;

                        var storageBlockData = GameAssets.GetBlockDataById(blockId) as StorageBlockData;

                        var size = new Vector2Byte(8, 3);


                        if (storageBlockData != null)
                        {

                            size = storageBlockData.Size;
                        }
                        else
                        {
                            Debug.Error($"[NBTHelper] - TagToChunk: StorageBlockData is null for block id: {blockId} at pos {pos} in chunk {ix},{iy},{iz}." +
                                $" Using size from NBT: {size.X},{size.Y}");
                        }

                        Storage newStorage = new Storage(size.X, size.Y);

                        if (storage.TryGetValue<StringTag>(NBTKey.STORAGE.name, out var nameTag))
                        {
                            var val = nameTag.Value;

                            newStorage.Name = val;

                        }


                        foreach (var slotData in storage.Get<LongArrayTag>(NBTKey.STORAGE.slots_data))
                        {
                            PackingTools.UnpackShorts(slotData, out var paletteId, out var count, out var posX, out var posY);

                            var itemIdStr = paletteitemsString[paletteId];


                            var item = GameAssets.GetItemByFullID(itemIdStr);

                            if (item != null)
                            {
                                var slot = newStorage.GetSlot(posX, posY);

                                if (slot != null)
                                {
                                    slot.SetData(item, (byte)count);
                                }
                            }

                        }


                        Block block = blocks[pos.X, pos.Y, pos.Z];

                        if (block != null)
                        {
                            StorageBlock? sBlock = block as StorageBlock;

                            if (sBlock != null)
                            {
                                sBlock.Storage = newStorage;
                                sBlock.SetPositionInChunk(pos);
                            }
                        }
                    }


                }
            }

        }




        Chunk chunk = new Chunk(new Vector3SByte(ix, iy, iz), spaceEntity, blocks, true, false);

        return chunk;
    }



    private static void GetCoordinatesFrom1DIndex(int index, int size, out byte x, out byte y, out byte z)
    {
        x = (byte)(index % size);
        y = (byte)(index / size % size);
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
        return x + y * chunkSize + z * chunkSize * chunkSize;
    }
}
