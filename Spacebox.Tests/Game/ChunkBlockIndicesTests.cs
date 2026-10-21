using SharpNBT;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game;

public class ChunkBlockIndicesTests
{
    private const short FirstId = 3500;
    private const int TypeCount = 300;
    private const int Cells = Chunk.Size * Chunk.Size * Chunk.Size;

    [Fact]
    public void SmallPalette_KeepsTheByteArray()
    {
        var tag = new CompoundTag("Chunk");
        var indices = Pattern(256);

        ChunkBlockIndices.Write(tag, indices, 256);

        Assert.True(tag.ContainsKey(NBTKey.CHUNK.blocks));
        Assert.False(tag.ContainsKey(NBTKey.CHUNK.blocks_wide));
        Assert.Equal(indices, ChunkBlockIndices.Read(tag));
    }

    [Fact]
    public void LargePalette_UsesTheWideArray()
    {
        var tag = new CompoundTag("Chunk");
        var indices = Pattern(TypeCount);

        ChunkBlockIndices.Write(tag, indices, TypeCount);

        Assert.False(tag.ContainsKey(NBTKey.CHUNK.blocks));
        Assert.True(tag.ContainsKey(NBTKey.CHUNK.blocks_wide));
        Assert.Equal(indices, ChunkBlockIndices.Read(tag));
    }

    [Fact]
    public void ChunkWithMoreThan256Types_LoadsEveryBlock()
    {
        var palette = new ListTag(NBTKey.CHUNK.palette_blocks, TagType.String);
        var data = new BlockData[TypeCount];
        for (int i = 0; i < TypeCount; i++)
        {
            data[i] = new BlockData("wide" + i, "block") { Id = (short)(FirstId + i), Id_string = "widetest:block" + i, Durability = 1, Mass = 1 };
            GameAssets.Blocks[data[i].Id] = data[i];
            GameAssets.AddBlockString(data[i].Id_string, data[i]);
            palette.Add(new StringTag(null, data[i].Id_string));
        }

        try
        {
            var tag = ChunkTag(palette);
            ChunkBlockIndices.Write(tag, Pattern(TypeCount), TypeCount);

            var blocks = NBTHelper.TagToChunkBlocks(tag, out _);

            Assert.Equal(FirstId, blocks[0, 0, 0].Id);
            Assert.Equal((short)(FirstId + 299), blocks[0, 9, 11].Id);
            int last = Cells - 1;
            Assert.Equal((short)(FirstId + last % TypeCount), blocks[Chunk.Size - 1, Chunk.Size - 1, Chunk.Size - 1].Id);
        }
        finally
        {
            foreach (var entry in data)
            {
                GameAssets.Blocks.Remove(entry.Id);
                GameAssets.BlocksStr.Remove(entry.Id_string);
            }
        }
    }

    private static int[] Pattern(int types)
    {
        var indices = new int[Cells];
        for (int i = 0; i < Cells; i++) indices[i] = i % types;
        return indices;
    }

    private static CompoundTag ChunkTag(ListTag palette)
    {
        var tag = new CompoundTag("Chunk");
        tag.Add(new ByteTag(NBTKey.CHUNK.index_x, 0));
        tag.Add(new ByteTag(NBTKey.CHUNK.index_y, 0));
        tag.Add(new ByteTag(NBTKey.CHUNK.index_z, 0));
        tag.Add(palette);
        tag.Add(new ListTag(NBTKey.CHUNK.palette_items, TagType.String));
        tag.Add(new IntArrayTag(NBTKey.CHUNK.directions, Array.Empty<int>()));
        tag.Add(new IntArrayTag(NBTKey.CHUNK.rotations, Array.Empty<int>()));
        tag.Add(new ListTag(NBTKey.CHUNK.storages, TagType.Compound));
        return tag;
    }
}
