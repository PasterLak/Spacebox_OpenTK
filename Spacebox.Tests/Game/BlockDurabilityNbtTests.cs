using Engine;
using SharpNBT;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game;
using Spacebox.Game.Resource;

namespace Spacebox.Tests;

public class BlockDurabilityNbtTests
{
    private const short StoneId = 4090;
    private const string StoneFullId = "durtest:stone";
    private const byte MaxDurability = 40;

    private static readonly BlockData StoneData = new BlockData("stone", "block")
    {
        Id = StoneId,
        Id_string = StoneFullId,
        Durability = MaxDurability,
        Mass = 1
    };

    private static Block Stone() => new Block(StoneData);

    [Fact]
    public void Collect_SkipsAirAndUndamagedBlocks()
    {
        var entries = new List<int>();
        var damaged = Stone();
        damaged.Durability = 12;

        BlockDurabilityNbt.Collect(entries, 0, Stone(), MaxDurability);
        BlockDurabilityNbt.Collect(entries, 1, new Block(0, Block.DefaultDirection, 0, 0, true, true, false, false), MaxDurability);
        BlockDurabilityNbt.Collect(entries, 2, damaged, MaxDurability);

        Assert.Equal(new[] { 2, 12 }, entries);
    }

    [Fact]
    public void Write_AddsNoTagWhenNothingIsDamaged()
    {
        var root = new CompoundTag("Chunk");

        BlockDurabilityNbt.Write(root, new List<int>());

        Assert.False(root.ContainsKey(NBTKey.CHUNK.durabilities));
    }

    [Fact]
    public void ChunkTag_RoundTripsDamagedDurability()
    {
        Register();
        try
        {
            var tag = StoneChunkTag();
            var entries = new List<int>();
            var damaged = Stone();
            damaged.Durability = 7;
            BlockDurabilityNbt.Collect(entries, NBTHelper.GetArrayIndex(1, 2, 3, Chunk.Size), damaged, MaxDurability);
            BlockDurabilityNbt.Write(tag, entries);

            var blocks = NBTHelper.TagToChunkBlocks(tag, out _);

            Assert.Equal(7, blocks[1, 2, 3].Durability);
            Assert.Equal(MaxDurability, blocks[0, 0, 0].Durability);
            Assert.Equal(MaxDurability, blocks[1, 2, 4].Durability);
        }
        finally
        {
            Unregister();
        }
    }

    [Fact]
    public void OldChunkTagWithoutDurabilities_LoadsWithFullDurability()
    {
        Register();
        try
        {
            var blocks = NBTHelper.TagToChunkBlocks(StoneChunkTag(), out _);

            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
                Assert.Equal(MaxDurability, blocks[x, y, z].Durability);
        }
        finally
        {
            Unregister();
        }
    }

    [Fact]
    public void Read_IgnoresBadIndicesAndValuesAboveTheMaximum()
    {
        var blocks = new Block[Chunk.Size, Chunk.Size, Chunk.Size];
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
            blocks[x, y, z] = Stone();

        var tag = new CompoundTag("Chunk");
        tag.Add(new IntArrayTag(NBTKey.CHUNK.durabilities, new[] { -1, 3, 999999, 3, 5, 200, 6, 4, 7 }));

        BlockDurabilityNbt.Read(tag, blocks);

        Assert.Equal(MaxDurability, blocks[0, 0, 5].Durability);
        Assert.Equal(4, blocks[0, 0, 6].Durability);
        Assert.Equal(MaxDurability, blocks[0, 0, 7].Durability);
    }

    private static CompoundTag StoneChunkTag()
    {
        var tag = new CompoundTag("Chunk");
        tag.Add(new ByteTag(NBTKey.CHUNK.index_x, 0));
        tag.Add(new ByteTag(NBTKey.CHUNK.index_y, 0));
        tag.Add(new ByteTag(NBTKey.CHUNK.index_z, 0));
        tag.Add(new ByteArrayTag(NBTKey.CHUNK.blocks, new byte[Chunk.Size * Chunk.Size * Chunk.Size]));
        tag.Add(new ListTag(NBTKey.CHUNK.palette_blocks, TagType.String) { new StringTag(null, StoneFullId) });
        tag.Add(new ListTag(NBTKey.CHUNK.palette_items, TagType.String));
        tag.Add(new IntArrayTag(NBTKey.CHUNK.directions, Array.Empty<int>()));
        tag.Add(new IntArrayTag(NBTKey.CHUNK.rotations, Array.Empty<int>()));
        tag.Add(new ListTag(NBTKey.CHUNK.storages, TagType.Compound));
        return tag;
    }

    private static void Register()
    {
        GameAssets.Blocks[StoneId] = StoneData;
        GameAssets.AddBlockString(StoneFullId, StoneData);
    }

    private static void Unregister()
    {
        GameAssets.Blocks.Remove(StoneId);
        GameAssets.BlocksStr.Remove(StoneFullId);
    }
}
