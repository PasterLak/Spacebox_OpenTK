using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.ShipViewer;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipImportTag
{
    public const int MaxChunks = 4096;

    public static CompoundTag Build(CompoundTag shipEntity, long id, Vector3i sector, Vector3 local, Quaternion rotation,
        Func<string, bool> isKnownBlock, string airId, SortedDictionary<string, int> missing)
    {
        var copy = ShipFileCodec.Clone(shipEntity);
        var root = new CompoundTag(null)
        {
            new LongTag(NBTKey.ENTITY.id, id),
            new StringTag(NBTKey.ENTITY.name, id.ToString()),
            new ByteTag(NBTKey.ENTITY.only_changed_chunks, 0),
            new FloatTag(NBTKey.ENTITY.rotation_x, rotation.X),
            new FloatTag(NBTKey.ENTITY.rotation_y, rotation.Y),
            new FloatTag(NBTKey.ENTITY.rotation_z, rotation.Z),
            new FloatTag(NBTKey.ENTITY.rotation_w, rotation.W)
        };
        EntityTagLocation.Write(root, sector, local);

        var chunks = copy.Get<ListTag>(NBTKey.ENTITY.chunks);
        copy.Remove(chunks);
        foreach (var entry in chunks)
            if (entry is CompoundTag chunk) ReplaceUnknownBlocks(chunk, isKnownBlock, airId, missing);
        root.Add(chunks);
        return root;
    }

    public static string Validate(CompoundTag shipEntity)
    {
        if (shipEntity == null || !shipEntity.TryGetValue<ListTag>(NBTKey.ENTITY.chunks, out var chunks)) return "The ship has no blocks";
        if (chunks.Count == 0) return "The ship has no blocks";
        if (chunks.Count > MaxChunks) return "The ship is too large";

        int half = Spacebox.Game.Generation.SpaceEntity.SizeChunksHalf;
        foreach (var chunk in ChunkTagAccess.Chunks(shipEntity))
        {
            var index = ShipTagReader.ReadChunkIndex(chunk);
            if (index.X < -half || index.Y < -half || index.Z < -half || index.X >= half || index.Y >= half || index.Z >= half) return "The ship has chunks outside the allowed size";
        }
        return null;
    }

    private static void ReplaceUnknownBlocks(CompoundTag chunk, Func<string, bool> isKnownBlock, string airId, SortedDictionary<string, int> missing)
    {
        if (!chunk.TryGetValue(NBTKey.CHUNK.palette_blocks, out ListTag palette)) return;

        foreach (var entry in palette)
        {
            if (entry is not StringTag text || ShipTagReader.IsEmpty(text.Value) || isKnownBlock(text.Value)) continue;
            missing[text.Value] = missing.GetValueOrDefault(text.Value) + 1;
            text.Value = airId;
        }
    }
}
