using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.ShipViewer;

public static class ShipTagReader
{
    public const int ChunkSize = 32;
    private const string AirId = "air";
    private const string VoidId = "void";

    public static bool IsPlayerBuilt(CompoundTag entity)
    {
        return entity != null && entity.TryGetValue<LongTag>(NBTKey.ENTITY.id, out var id) && id.Value < 0;
    }

    public static long ReadId(CompoundTag entity)
    {
        return entity != null && entity.TryGetValue<LongTag>(NBTKey.ENTITY.id, out var id) ? id.Value : 0;
    }

    public static string ReadName(CompoundTag entity, string fallback)
    {
        return entity != null && entity.TryGetValue<StringTag>(NBTKey.ENTITY.name, out var name) && !string.IsNullOrWhiteSpace(name.Value)
            ? name.Value
            : fallback;
    }

    public static Vector3i ReadChunkIndex(CompoundTag chunk)
    {
        return new Vector3i(
            PackingTools.ByteToSByte(chunk.Get<ByteTag>(NBTKey.CHUNK.index_x).Value),
            PackingTools.ByteToSByte(chunk.Get<ByteTag>(NBTKey.CHUNK.index_y).Value),
            PackingTools.ByteToSByte(chunk.Get<ByteTag>(NBTKey.CHUNK.index_z).Value));
    }

    public static bool IsEmpty(string fullId)
    {
        if (string.IsNullOrEmpty(fullId)) return true;
        int colon = fullId.IndexOf(':');
        string shortId = colon < 0 ? fullId : fullId.Substring(colon + 1);
        return string.Equals(shortId, AirId, StringComparison.OrdinalIgnoreCase) || string.Equals(shortId, VoidId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsBlock(CompoundTag entity, Func<string, bool> match)
    {
        foreach (var chunk in ChunkTagAccess.Chunks(entity))
        {
            var palette = ChunkTagAccess.BlockPalette(chunk);
            var wanted = new bool[palette.Count];
            bool any = false;
            for (int i = 0; i < wanted.Length; i++)
            {
                wanted[i] = match(palette[i]);
                any |= wanted[i];
            }
            if (!any) continue;

            var cells = ChunkBlockIndices.Read(chunk);
            for (int i = 0; i < cells.Length; i++)
            {
                int paletteIndex = cells[i];
                if (paletteIndex >= 0 && paletteIndex < wanted.Length && wanted[paletteIndex]) return true;
            }
        }
        return false;
    }

    public static ShipBlockStats Measure(CompoundTag entity)
    {
        int chunks = 0;
        int blocks = 0;
        var min = new Vector3i(int.MaxValue);
        var max = new Vector3i(int.MinValue);

        foreach (var chunk in ChunkTagAccess.Chunks(entity))
        {
            chunks++;
            var origin = ReadChunkIndex(chunk) * ChunkSize;
            var palette = ChunkTagAccess.BlockPalette(chunk);
            var solid = new bool[palette.Count];
            for (int i = 0; i < solid.Length; i++) solid[i] = !IsEmpty(palette[i]);

            var cells = ChunkBlockIndices.Read(chunk);
            for (int i = 0; i < cells.Length; i++)
            {
                int paletteIndex = cells[i];
                if (paletteIndex < 0 || paletteIndex >= solid.Length || !solid[paletteIndex]) continue;

                blocks++;
                var cell = origin + CellOf(i);
                min = Vector3i.ComponentMin(min, cell);
                max = Vector3i.ComponentMax(max, cell + Vector3i.One);
            }
        }

        return blocks == 0 ? new ShipBlockStats(chunks, 0, Vector3i.Zero, Vector3i.Zero) : new ShipBlockStats(chunks, blocks, min, max);
    }

    public static Vector3i CellOf(int flatIndex)
    {
        return new Vector3i(flatIndex / (ChunkSize * ChunkSize), flatIndex / ChunkSize % ChunkSize, flatIndex % ChunkSize);
    }
}
