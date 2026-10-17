using Engine;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Resource;
using System;
using System.Collections.Generic;

namespace Spacebox.Game.Generation.Tools
{
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

            var listBlocks = new byte[SIZE * SIZE * SIZE];

            Dictionary<short, byte> sessionLocalIdToPaletteIndex = new Dictionary<short, byte>();
            List<string> paletteStrings = new List<string>();

            Dictionary<string, short> paletteItems = new Dictionary<string, short>();
            short indexInPaletteItems = 0;

            List<int> blockWithDirectionIDs = new List<int>();
            List<int> blockWithRotationIDs = new List<int>();

            var complexBlocksList = new ListTag(NBTKey.CHUNK.storages, TagType.Compound);

            int indexIn1D = 0;

            for (byte x = 0; x < SIZE; x++)
            {
                for (byte y = 0; y < SIZE; y++)
                {
                    for (byte z = 0; z < SIZE; z++)
                    {
                        var block = blocks[x, y, z];
                        short currentSessionId = block.Id;

                        if (!sessionLocalIdToPaletteIndex.TryGetValue(currentSessionId, out byte paletteIndex))
                        {
                            paletteIndex = (byte)paletteStrings.Count;
                            sessionLocalIdToPaletteIndex[currentSessionId] = paletteIndex;

                            var blockFullId = GameAssets.GetBlockFullId(block);
                            paletteStrings.Add(blockFullId);
                        }

                        listBlocks[indexIn1D] = paletteIndex;

                        if (block.Direction != Block.DefaultDirection)
                        {
                            blockWithDirectionIDs.Add(indexIn1D);
                            blockWithDirectionIDs.Add((byte)block.Direction);
                        }

                        if (block.Rotation != Block.DefaultRotation)
                        {
                            blockWithRotationIDs.Add(indexIn1D);
                            blockWithRotationIDs.Add((byte)block.Rotation);
                        }

                        var serializer = BlockSerializerRegistry.GetSerializer(block.GetType());
                        if (serializer != null)
                        {
                            CompoundTag complexTag = serializer.Serialize(block, indexIn1D, paletteItems, ref indexInPaletteItems);
                            if (complexTag != null)
                            {
                                complexBlocksList.Add(complexTag);
                            }
                        }

                        indexIn1D++;
                    }
                }
            }

            root.Add(new ByteArrayTag(NBTKey.CHUNK.blocks, listBlocks));

            var listPaletteBlocks = new ListTag(NBTKey.CHUNK.palette_blocks, TagType.String);
            for (int i = 0; i < paletteStrings.Count; i++)
            {
                listPaletteBlocks.Add(new StringTag(null, paletteStrings[i]));
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

            var listDirections = new IntArrayTag(NBTKey.CHUNK.directions, blockWithDirectionIDs.ToArray());
            root.Add(listDirections);

            var listRotations = new IntArrayTag(NBTKey.CHUNK.rotations, blockWithRotationIDs.ToArray());
            root.Add(listRotations);

            root.Add(complexBlocksList);

            return root;
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

            var listBlocks = tag.Get<ByteArrayTag>(NBTKey.CHUNK.blocks).ToArray();
            var listPaletteBlocks = tag.Get<ListTag>(NBTKey.CHUNK.palette_blocks);

            short[] mappedSessionIds = new short[listPaletteBlocks.Count];
            for (int i = 0; i < listPaletteBlocks.Count; i++)
            {
                string stringId = (listPaletteBlocks[i] as StringTag).Value;
                var blockData = GameAssets.GetBlockByFullID(stringId);
                mappedSessionIds[i] = blockData != null ? blockData.Id : (short)0;
            }

            short[] ids = new short[SIZE * SIZE * SIZE];
            for (int i = 0; i < listBlocks.Length; i++)
            {
                ids[i] = mappedSessionIds[listBlocks[i]];
            }

            Block[,,] blocks = ReconstructBlocksFrom1D(ids);

            if (tag.TryGetValue<IntArrayTag>(NBTKey.CHUNK.directions, out var listDirections))
            {
                var arr = listDirections.ToArray();
                for (int i = 0; i < arr.Length; i += 2)
                {
                    GetCoordinatesFrom1DIndex(arr[i], SIZE, out byte x, out byte y, out byte z);
                    blocks[x, y, z].Direction = (Direction)arr[i + 1];
                }
            }

            if (tag.TryGetValue<IntArrayTag>(NBTKey.CHUNK.rotations, out var listRotations))
            {
                var arr = listRotations.ToArray();
                for (int i = 0; i < arr.Length; i += 2)
                {
                    GetCoordinatesFrom1DIndex(arr[i], SIZE, out byte x, out byte y, out byte z);
                    blocks[x, y, z].Rotation = (Rotation)arr[i + 1];
                }
            }

            string[] paletteItemsString = new string[0];
            if (tag.TryGetValue<ListTag>(NBTKey.CHUNK.palette_items, out var paletteItemsTag))
            {
                paletteItemsString = ListTagToPaletteItems(paletteItemsTag);
            }

            if (tag.TryGetValue<ListTag>(NBTKey.CHUNK.storages, out var complexBlocksTag))
            {
                foreach (CompoundTag complexTag in complexBlocksTag)
                {
                    if (complexTag.TryGetValue<IntTag>("flat_index", out var flatIndexTag))
                    {
                        GetCoordinatesFrom1DIndex(flatIndexTag.Value, SIZE, out byte x, out byte y, out byte z);
                        var block = blocks[x, y, z];

                        var serializer = BlockSerializerRegistry.GetSerializer(block.GetType());
                        if (serializer != null)
                        {
                            serializer.Deserialize(block, complexTag, paletteItemsString);

                            if (block is StorageBlock sBlock)
                            {
                                sBlock.SetPositionInChunk(new Vector3Byte(x, y, z));
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
            z = (byte)(index % size);
            y = (byte)((index / size) % size);
            x = (byte)(index / (size * size));
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
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        blocks[x, y, z] = GameAssets.CreateBlockFromId(ids[index]);
                        index++;
                    }
                }
            }
            return blocks;
        }

        public static int GetArrayIndex(byte x, byte y, byte z, byte chunkSize)
        {
            return z + (y * chunkSize) + (x * chunkSize * chunkSize);
        }
    }
}