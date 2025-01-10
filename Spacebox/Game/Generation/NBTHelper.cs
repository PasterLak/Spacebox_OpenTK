using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Common;
using System;

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

            foreach ( CompoundTag chunk in chunksList )
            {
                chunks.Add(TagToChunk(chunk, spaceEntity));
            }
            spaceEntity.AddChunks(chunks.ToArray(), false);
            return spaceEntity;

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
                if (chunksList[i].IsModified)
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

            List<int> idList = new List<int>();
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
                            listBlocks[indexIn1D] =  id;
                            id++;
                        }

                        if (block.Direction != Direction.Up)
                        {
                            idList.Add(GetArrayIndex(x,y,z, SIZE)); // indexIn1D
                            idList.Add((byte)block.Direction);
                            //Debug.Log($"Direction save index: {indexIn1D} row direction {(byte)block.Direction} end pos {x},{y},{z} block id: {block.BlockId}");
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

            var listRotations = new IntArrayTag("rotations", idList.Count);

            for (int x = 0; x < idList.Count; x++)
            {
                listRotations[x] = idList[x];
            }

            root.Add(listRotations);

            return root;
        }

        public static Chunk? TagToChunk(CompoundTag tag, SpaceEntity spaceEntity)
        {
            const byte SIZE = Chunk.Size;
            sbyte ix = ByteToSByte( tag.Get<ByteTag>("indexX").Value);
            sbyte iy = ByteToSByte(tag.Get<ByteTag>("indexY").Value);
            sbyte iz = ByteToSByte(tag.Get<ByteTag>("indexZ").Value);

            var listBlocks = tag.Get<IntArrayTag>("blocks").ToArray();
            var listPalette = tag.Get<IntArrayTag>("palette").ToArray(); 
            var listRotations = tag.Get<IntArrayTag>("rotations").ToArray();

            short[] ids = new short[SIZE * SIZE * SIZE];

            if(ids.Length != listBlocks.Length)
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

                byte x = (byte)(index1D % SIZE);
                byte y = (byte)((index1D / SIZE) % SIZE);
                byte z = (byte)(index1D / (SIZE * SIZE));

                if (x < SIZE && y < SIZE && z < SIZE)
                {
                    blocks[x, y, z].Direction = (Direction)direction;
                }
                //blocks[x, y, z].Color = new Vector3(1,0,0);
                //blocks[x, y, z].BlockId = ids[(int)index1D];
                //Debug.Log($"Direction index: {index1D} row direction {direction} end pos {x},{y},{z}");
                //Debug.Log($"Block direction: {blocks[x, y, z].Direction} Id: {blocks[x, y, z].BlockId}");
            }


            Chunk chunk = new Chunk(new Vector3SByte(ix,iy,iz) , spaceEntity, blocks, true);

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
                        blocks[x, y, z] = GameBlocks.CreateBlockFromId(ids[index]);
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
