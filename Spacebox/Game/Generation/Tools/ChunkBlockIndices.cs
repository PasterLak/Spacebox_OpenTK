using SharpNBT;

namespace Spacebox.Game.Generation.Tools;

public static class ChunkBlockIndices
{
    public const int MaxBytePalette = byte.MaxValue + 1;

    public static void Write(CompoundTag chunk, int[] indices, int paletteCount)
    {
        if (paletteCount <= MaxBytePalette)
        {
            var bytes = new byte[indices.Length];
            for (int i = 0; i < indices.Length; i++) bytes[i] = (byte)indices[i];
            chunk.Add(new ByteArrayTag(NBTKey.CHUNK.blocks, bytes));
            return;
        }

        chunk.Add(new IntArrayTag(NBTKey.CHUNK.blocks_wide, indices));
    }

    public static int[] Read(CompoundTag chunk)
    {
        if (chunk.TryGetValue<IntArrayTag>(NBTKey.CHUNK.blocks_wide, out var wide)) return wide.ToArray();

        var bytes = chunk.Get<ByteArrayTag>(NBTKey.CHUNK.blocks).ToArray();
        var indices = new int[bytes.Length];
        for (int i = 0; i < bytes.Length; i++) indices[i] = bytes[i];
        return indices;
    }
}
