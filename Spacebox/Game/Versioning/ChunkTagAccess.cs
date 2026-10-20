using SharpNBT;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Versioning;

public static class ChunkTagAccess
{
    public static IEnumerable<CompoundTag> Chunks(CompoundTag entity)
    {
        if (entity == null || !entity.TryGetValue(NBTKey.ENTITY.chunks, out ListTag chunks)) yield break;

        foreach (var chunk in chunks)
        {
            if (chunk is CompoundTag compound) yield return compound;
        }
    }

    public static IReadOnlyList<string> BlockPalette(CompoundTag chunk) => ReadPalette(chunk, NBTKey.CHUNK.palette_blocks);

    public static IReadOnlyList<string> ItemPalette(CompoundTag chunk) => ReadPalette(chunk, NBTKey.CHUNK.palette_items);

    public static IReadOnlyList<string> ReadPalette(CompoundTag chunk, string key)
    {
        var result = new List<string>();
        if (chunk == null || !chunk.TryGetValue(key, out ListTag palette)) return result;

        foreach (var entry in palette)
        {
            if (entry is StringTag text) result.Add(text.Value);
        }
        return result;
    }

    public static bool RemapPalette(CompoundTag chunk, string key, IdRemap remap)
    {
        if (chunk == null || remap == null || remap.IsEmpty || !chunk.TryGetValue(key, out ListTag palette)) return false;

        bool changed = false;
        foreach (var entry in palette)
        {
            if (entry is StringTag text && remap.TryMap(text.Value, out var mapped))
            {
                text.Value = mapped;
                changed = true;
            }
        }
        return changed;
    }
}
