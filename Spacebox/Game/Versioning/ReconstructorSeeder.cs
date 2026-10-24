using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Player.Respawn;

namespace Spacebox.Game.Versioning;

public static class ReconstructorSeeder
{
    private const int ChunkSize = 32;

    public static int Apply(WorldUpgradeContext world, IReadOnlyCollection<string> reconstructorIds)
    {
        if (!world.HasFolder) return 0;

        var records = new Dictionary<ReconstructorKey, ReconstructorRecord>();
        foreach (var record in ReconstructorRecordsFile.Read(world.WorldFolder)) records[record.Key] = record;

        int found = 0;
        string folder = EntityFiles.FolderOf(world.WorldFolder);
        if (Directory.Exists(folder))
        {
            foreach (var file in Directory.GetFiles(folder, "*" + EntityFiles.Extension))
            {
                var entity = NbtFileEditor.TryRead(file);
                var list = new List<ReconstructorRecord>();
                Collect(entity, reconstructorIds, list);
                foreach (var record in list)
                {
                    if (records.TryAdd(record.Key, record)) found++;
                }
            }
        }

        if (found > 0) ReconstructorRecordsFile.Write(world.WorldFolder, records.Values);
        world.Count("DNA reconstructors registered", found);
        return found;
    }

    public static void Collect(CompoundTag entity, IReadOnlyCollection<string> reconstructorIds, List<ReconstructorRecord> output)
    {
        if (entity == null || !entity.TryGetValue<LongTag>(NBTKey.ENTITY.id, out var id)) return;
        if (!EntityTagLocation.TryReadLocal(entity, out var local)) return;

        EntityTagLocation.TryReadSector(entity, out var sector);
        var position = SpaceMath.Sector.GetSectorPosition(sector) + local;
        var orientation = ReadOrientation(entity);

        foreach (var chunk in ChunkTagAccess.Chunks(entity))
        {
            var palette = ChunkTagAccess.BlockPalette(chunk);
            var reconstructorIndices = new HashSet<int>();
            for (int i = 0; i < palette.Count; i++)
            {
                if (Contains(reconstructorIds, palette[i])) reconstructorIndices.Add(i);
            }
            if (reconstructorIndices.Count == 0) continue;

            var chunkIndex = ReadChunkIndex(chunk);
            var dna = ReadDnaCounts(chunk);
            var cells = ChunkBlockIndices.Read(chunk);
            for (int flat = 0; flat < cells.Length; flat++)
            {
                if (!reconstructorIndices.Contains(cells[flat])) continue;

                int x = flat / (ChunkSize * ChunkSize);
                int y = flat / ChunkSize % ChunkSize;
                int z = flat % ChunkSize;
                var cell = new Vector3i(chunkIndex.X * ChunkSize + x, chunkIndex.Y * ChunkSize + y, chunkIndex.Z * ChunkSize + z);
                var center = position + Vector3.Transform(new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f), orientation);

                dna.TryGetValue(flat, out int count);
                output.Add(ReconstructorRecord.At(new ReconstructorKey(id.Value, cell), center, count, true));
            }
        }
    }

    private static bool Contains(IReadOnlyCollection<string> ids, string id)
    {
        foreach (var candidate in ids)
        {
            if (string.Equals(candidate, id, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static Vector3i ReadChunkIndex(CompoundTag chunk)
    {
        return new Vector3i(
            PackingTools.ByteToSByte(chunk.Get<ByteTag>(NBTKey.CHUNK.index_x).Value),
            PackingTools.ByteToSByte(chunk.Get<ByteTag>(NBTKey.CHUNK.index_y).Value),
            PackingTools.ByteToSByte(chunk.Get<ByteTag>(NBTKey.CHUNK.index_z).Value));
    }

    private static Dictionary<int, int> ReadDnaCounts(CompoundTag chunk)
    {
        var counts = new Dictionary<int, int>();
        if (!chunk.TryGetValue<ListTag>(NBTKey.CHUNK.storages, out var storages)) return counts;

        foreach (var child in storages)
        {
            if (child is not CompoundTag storage) continue;
            if (!storage.TryGetValue<IntTag>("flat_index", out var flat) || !storage.TryGetValue<IntTag>("slot_data", out var slot)) continue;

            PackingTools.UnpackShorts(slot.Value, out _, out short count);
            counts[flat.Value] = Math.Max(0, (int)count);
        }
        return counts;
    }

    private static Quaternion ReadOrientation(CompoundTag entity)
    {
        if (!entity.TryGetValue<FloatTag>(NBTKey.ENTITY.rotation_x, out var x)
            || !entity.TryGetValue<FloatTag>(NBTKey.ENTITY.rotation_y, out var y)
            || !entity.TryGetValue<FloatTag>(NBTKey.ENTITY.rotation_z, out var z)
            || !entity.TryGetValue<FloatTag>(NBTKey.ENTITY.rotation_w, out var w))
            return Quaternion.Identity;

        var rotation = new Quaternion(x.Value, y.Value, z.Value, w.Value);
        return rotation.LengthSquared > 1e-6f ? rotation.Normalized() : Quaternion.Identity;
    }
}
