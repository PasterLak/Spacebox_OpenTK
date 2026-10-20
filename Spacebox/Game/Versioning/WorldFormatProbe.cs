using SharpNBT;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Versioning;

public enum WorldSaveFormat
{
    Current,
    WorldSpacePositions,
    Legacy
}

public static class WorldFormatProbe
{
    private const string LegacyEntityPosition = "worldX";
    private const string LegacyChunkIndex = "indexX";
    private const string WorldSpacePosition = "world_x";

    public static WorldSaveFormat Detect(string worldFolder)
    {
        var format = WorldSaveFormat.Current;
        foreach (var file in WorldFileLayout.EntityFilePaths(worldFolder))
        {
            var entity = NbtFileEditor.Read(file);
            var detected = Detect(entity);
            if (detected == WorldSaveFormat.Legacy) return detected;
            if (detected == WorldSaveFormat.WorldSpacePositions) format = detected;
        }
        return format;
    }

    public static WorldSaveFormat Detect(CompoundTag entity)
    {
        if (entity == null) return WorldSaveFormat.Current;
        if (entity.ContainsKey(LegacyEntityPosition)) return WorldSaveFormat.Legacy;

        foreach (var chunk in ChunkTagAccess.Chunks(entity))
        {
            if (chunk.ContainsKey(LegacyChunkIndex) || !chunk.ContainsKey(NBTKey.CHUNK.palette_blocks)) return WorldSaveFormat.Legacy;
        }

        return entity.ContainsKey(WorldSpacePosition) && !entity.ContainsKey(NBTKey.ENTITY.local_x)
            ? WorldSaveFormat.WorldSpacePositions
            : WorldSaveFormat.Current;
    }
}
