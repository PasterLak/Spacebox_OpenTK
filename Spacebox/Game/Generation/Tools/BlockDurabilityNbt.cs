using SharpNBT;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public static class BlockDurabilityNbt
{
    public static void Collect(List<int> entries, int flatIndex, Block block, byte maxDurability)
    {
        if (block.IsAir || block.Durability >= maxDurability) return;

        entries.Add(flatIndex);
        entries.Add(block.Durability);
    }

    public static void Write(CompoundTag root, List<int> entries)
    {
        if (entries.Count == 0) return;
        root.Add(new IntArrayTag(NBTKey.CHUNK.durabilities, entries.ToArray()));
    }

    public static void Read(CompoundTag tag, Block[,,] blocks)
    {
        if (!tag.TryGetValue<IntArrayTag>(NBTKey.CHUNK.durabilities, out var arrayTag)) return;

        var entries = arrayTag.ToArray();
        int size = Chunk.Size;
        int volume = size * size * size;

        for (int i = 0; i + 1 < entries.Length; i += 2)
        {
            int flatIndex = entries[i];
            if (flatIndex < 0 || flatIndex >= volume) continue;

            int z = flatIndex % size;
            int y = flatIndex / size % size;
            int x = flatIndex / (size * size);

            var block = blocks[x, y, z];
            if (block == null || block.IsAir) continue;

            int saved = entries[i + 1];
            if (saved < 0 || saved >= block.Durability) continue;

            block.Durability = (byte)saved;
        }
    }
}
