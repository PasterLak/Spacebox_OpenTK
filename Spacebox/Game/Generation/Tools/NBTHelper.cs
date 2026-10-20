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

        public static SpaceEntity? TagToSpaceEntity(CompoundTag tag, Sector sector, NotGeneratedEntity generationData = null)
        {
            if (tag == null) return null;

            long id = tag.Get<LongTag>(NBTKey.ENTITY.id);
            string file_name = tag.Get<StringTag>(NBTKey.ENTITY.name);

            var x = tag.Get<FloatTag>(NBTKey.ENTITY.local_x);
            var y = tag.Get<FloatTag>(NBTKey.ENTITY.local_y);
            var z = tag.Get<FloatTag>(NBTKey.ENTITY.local_z);

            var worldPos = sector.LocalToWorldPosition(new Vector3(x, y, z));

            SpaceEntity spaceEntity;
            bool onlyChangedChunks = tag.TryGetValue<ByteTag>(NBTKey.ENTITY.only_changed_chunks, out var onlyChangedTag) && onlyChangedTag.Value == 1;

            if (id >= 0)
            {
                Asteroid asteroid;

                if (onlyChangedChunks && (generationData?.asteroid == null || generationData.Id != id))
                    generationData = ReadAsteroidGenerationData(tag, id, generationData) ?? generationData;

                if (onlyChangedChunks && generationData?.asteroid != null && generationData.Id == id)
                {
                    generationData.positionWorld = worldPos;
                    asteroid = new Asteroid(generationData, sector, true);
                    asteroid.OnGenerate();
                }
                else
                {
                    var stubData = new NotGeneratedEntity
                    {
                        Id = id,
                        positionWorld = worldPos
                    };

                    asteroid = new Asteroid(stubData, sector, false);

                    if (onlyChangedChunks)
                    {
                        asteroid.MarkSavedWithoutGenerator();
                        Debug.Warning($"[NBTHelper] Asteroid {id} has no generation data; only its saved chunks are loaded");
                    }
                }

                asteroid.IsGenerated = true;
                spaceEntity = asteroid;
            }
            else
            {
                spaceEntity = new SpaceEntity(id, worldPos, sector);
            }

            spaceEntity.Name = file_name;
            spaceEntity.IsGenerated = true;
            spaceEntity.SavedSectorIndex = sector.PositionIndex;

            if (tag.ContainsKey(NBTKey.ENTITY.rotation_x))
            {
                var rx = tag.Get<FloatTag>(NBTKey.ENTITY.rotation_x);
                var ry = tag.Get<FloatTag>(NBTKey.ENTITY.rotation_y);
                var rz = tag.Get<FloatTag>(NBTKey.ENTITY.rotation_z);
                var rw = tag.Get<FloatTag>(NBTKey.ENTITY.rotation_w);

                spaceEntity.SetOrientation(new Quaternion(rx, ry, rz, rw));
            }

            var chunksList = tag.Get<ListTag>(NBTKey.ENTITY.chunks);
            var chunks = new List<Chunk>();
            var completeChunks = new List<Vector3SByte>();
            var decoded = new Dictionary<Vector3SByte, Block[,,]>();

            foreach (CompoundTag chunkTag in chunksList)
            {
                var blocks = TagToChunkBlocks(chunkTag, out var chunkIndex);
                decoded[chunkIndex] = blocks;

                bool needsGeneration = chunkTag.TryGetValue<ByteTag>(NBTKey.CHUNK.needs_generation, out var needsTag) && needsTag.Value == 1;
                if (!needsGeneration)
                    completeChunks.Add(chunkIndex);
            }

            var repair = MultiblockRepair.ForCurrentGameSet().Repair(decoded);
            if (repair.Changed)
                Debug.Warning($"[NBTHelper] Entity {spaceEntity.EntityID}: multi-cell blocks completed {repair.Completed}, removed {repair.Removed}, orphan cells removed {repair.OrphanPartsRemoved}");

            foreach (var pair in decoded)
            {
                var chunk = new Chunk(pair.Key, spaceEntity, pair.Value, true, false);
                if (repair.Changed) chunk.IsModified = true;
                chunks.Add(chunk);
            }

            spaceEntity.AddChunks(chunks.ToArray(), false);

            if (onlyChangedChunks && spaceEntity is Asteroid restoredAsteroid)
                restoredAsteroid.RestoreSavedState(completeChunks, ReadChunkIndices(tag, NBTKey.ENTITY.removed_chunks));

            if (spaceEntity.Ship != null)
            {
                spaceEntity.Ship.Motion.Velocity = ReadVector(tag, NBTKey.ENTITY.velocity);
                spaceEntity.Ship.Motion.AngularVelocity = ReadVector(tag, NBTKey.ENTITY.angular_velocity);
            }

            return spaceEntity;
        }

        private static NotGeneratedEntity ReadAsteroidGenerationData(CompoundTag tag, long id, NotGeneratedEntity existing)
        {
            if (!tag.TryGetValue<StringTag>(NBTKey.ENTITY.asteroid_type, out var typeTag)) return null;

            var asteroidData = World.WorldGenerator?.GetAsteroid(typeTag.Value);
            if (asteroidData == null)
            {
                Debug.Warning($"[NBTHelper] Asteroid type '{typeTag.Value}' of entity {id} is not in this GameSet");
                return null;
            }

            int radius = tag.TryGetValue<IntTag>(NBTKey.ENTITY.asteroid_radius, out var radiusTag) ? radiusTag.Value : asteroidData.MinRadius;
            var data = existing != null && existing.Id == id ? existing : new NotGeneratedEntity { Id = id, FileName = existing?.FileName };
            data.asteroid = asteroidData;
            data.radiusBlocks = Math.Max(1, radius);
            return data;
        }

        public static CompoundTag SpaceEntityToTag(SpaceEntity entity)
        {
            return SpaceEntityToTag(entity, entity.Sector.PositionWorld);
        }

        public static CompoundTag SpaceEntityToTag(SpaceEntity entity, Vector3 sectorOrigin)
        {
            var root = new CompoundTag(entity.GetType().Name);

            root.Add(new LongTag(NBTKey.ENTITY.id, entity.EntityID));
            root.Add(new StringTag(NBTKey.ENTITY.name, entity.Name));

            var localPos = entity.PositionWorld - sectorOrigin;

            root.Add(new FloatTag(NBTKey.ENTITY.local_x, localPos.X));
            root.Add(new FloatTag(NBTKey.ENTITY.local_y, localPos.Y));
            root.Add(new FloatTag(NBTKey.ENTITY.local_z, localPos.Z));

            Quaternion rot = entity.Orientation;

            root.Add(new FloatTag(NBTKey.ENTITY.rotation_x, rot.X));
            root.Add(new FloatTag(NBTKey.ENTITY.rotation_y, rot.Y));
            root.Add(new FloatTag(NBTKey.ENTITY.rotation_z, rot.Z));
            root.Add(new FloatTag(NBTKey.ENTITY.rotation_w, rot.W));

            bool onlyChangedChunks = entity.SavesOnlyChangedChunks;
            root.Add(new ByteTag(NBTKey.ENTITY.only_changed_chunks, (byte)(onlyChangedChunks ? 1 : 0)));

            if (entity is Asteroid { NotGeneratedEntity.asteroid: { } asteroidData } asteroid)
            {
                root.Add(new StringTag(NBTKey.ENTITY.asteroid_type, asteroidData.IdString));
                root.Add(new IntTag(NBTKey.ENTITY.asteroid_radius, asteroid.NotGeneratedEntity.radiusBlocks));
            }

            if (entity.Ship != null && entity.Ship.Motion.IsMoving)
            {
                WriteVector(root, NBTKey.ENTITY.velocity, entity.Ship.Motion.Velocity);
                WriteVector(root, NBTKey.ENTITY.angular_velocity, entity.Ship.Motion.AngularVelocity);
            }

            var chunks = new ListTag(NBTKey.ENTITY.chunks, TagType.Compound);
            var chunksList = entity.Chunks;

            for (int i = 0; i < chunksList.Count; i++)
            {
                var chunk = chunksList[i];
                if (onlyChangedChunks && !chunk.ShouldBeSaved) continue;

                var chunkTag = ChunkToTag(chunk);
                if (onlyChangedChunks && !entity.IsChunkGenerated(chunk.PositionIndex))
                    chunkTag.Add(new ByteTag(NBTKey.CHUNK.needs_generation, 1));

                chunks.Add(chunkTag);
            }

            root.Add(chunks);

            if (onlyChangedChunks)
                root.Add(new IntArrayTag(NBTKey.ENTITY.removed_chunks, PackChunkIndices(entity.RemovedChunks)));

            return root;
        }

        private static void WriteVector(CompoundTag root, string key, Vector3 value)
        {
            root.Add(new ListTag(key, TagType.Float) { new FloatTag(null, value.X), new FloatTag(null, value.Y), new FloatTag(null, value.Z) });
        }

        private static Vector3 ReadVector(CompoundTag tag, string key)
        {
            if (!tag.TryGetValue<ListTag>(key, out var list) || list.Count < 3) return Vector3.Zero;
            return new Vector3(((FloatTag)list[0]).Value, ((FloatTag)list[1]).Value, ((FloatTag)list[2]).Value);
        }

        private static int[] PackChunkIndices(IReadOnlyCollection<Vector3SByte> indices)
        {
            var packed = new int[indices.Count];
            int i = 0;
            foreach (var index in indices)
            {
                packed[i++] = (PackingTools.SByteToByte(index.X) << 16) | (PackingTools.SByteToByte(index.Y) << 8) | PackingTools.SByteToByte(index.Z);
            }
            return packed;
        }

        private static List<Vector3SByte> ReadChunkIndices(CompoundTag tag, string key)
        {
            var result = new List<Vector3SByte>();
            if (!tag.TryGetValue<IntArrayTag>(key, out var arrayTag)) return result;

            foreach (int packed in arrayTag)
            {
                result.Add(new Vector3SByte(
                    PackingTools.ByteToSByte((byte)((packed >> 16) & 0xFF)),
                    PackingTools.ByteToSByte((byte)((packed >> 8) & 0xFF)),
                    PackingTools.ByteToSByte((byte)(packed & 0xFF))));
            }
            return result;
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
            List<int> multiblockOffsets = new List<int>();

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

                        if (block.HasMultiblockOffset)
                        {
                            var offset = block.MultiblockOffset;
                            multiblockOffsets.Add(indexIn1D);
                            multiblockOffsets.Add(offset.X | (offset.Y << 4) | (offset.Z << 8));
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

            if (multiblockOffsets.Count > 0)
                root.Add(new IntArrayTag(NBTKey.CHUNK.multiblock_offsets, multiblockOffsets.ToArray()));

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
            var blocks = TagToChunkBlocks(tag, out var index);
            return new Chunk(index, spaceEntity, blocks, true, false);
        }

        public static Block[,,] TagToChunkBlocks(CompoundTag tag, out Vector3SByte chunkIndex)
        {
            const byte SIZE = Chunk.Size;
            sbyte ix = PackingTools.ByteToSByte(tag.Get<ByteTag>(NBTKey.CHUNK.index_x).Value);
            sbyte iy = PackingTools.ByteToSByte(tag.Get<ByteTag>(NBTKey.CHUNK.index_y).Value);
            sbyte iz = PackingTools.ByteToSByte(tag.Get<ByteTag>(NBTKey.CHUNK.index_z).Value);
            chunkIndex = new Vector3SByte(ix, iy, iz);

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

            if (tag.TryGetValue<IntArrayTag>(NBTKey.CHUNK.multiblock_offsets, out var listMultiblockOffsets))
            {
                var arr = listMultiblockOffsets.ToArray();
                for (int i = 0; i + 1 < arr.Length; i += 2)
                {
                    GetCoordinatesFrom1DIndex(arr[i], SIZE, out byte x, out byte y, out byte z);
                    int packed = arr[i + 1];
                    blocks[x, y, z].MultiblockOffset = new Vector3Byte((byte)(packed & 0xF), (byte)((packed >> 4) & 0xF), (byte)((packed >> 8) & 0xF));
                    if (blocks[x, y, z].HasMultiblockOffset) blocks[x, y, z].ConfigureAsMultiblockPart();
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

            return blocks;
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