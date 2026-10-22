using Engine;
using Engine.Utils;
using OpenTK.Mathematics;
using SharpNBT;

namespace Spacebox.Game.Generation.Tools;

public static class EntityCatalogFile
{
    public const string FileName = "entities.index";
    private const string RootName = "EntityIndex";
    private const string EntitiesKey = "entities";

    public static string PathOf(string worldFolder) => Path.Combine(worldFolder, FileName);

    public static CompoundTag ToTag(IEnumerable<EntityRecord> records)
    {
        var root = new CompoundTag(RootName);
        var list = new ListTag(EntitiesKey, TagType.Compound);
        foreach (var record in records)
        {
            var entry = new CompoundTag(null);
            entry.Add(new LongTag(NBTKey.ENTITY.id, record.Id));
            entry.Add(new StringTag(NBTKey.ENTITY.name, record.Name ?? record.Id.ToString()));
            EntityTagLocation.Write(entry, record.Sector, record.Local);
            list.Add(entry);
        }
        root.Add(list);
        return root;
    }

    public static List<EntityRecord> FromTag(CompoundTag root)
    {
        var records = new List<EntityRecord>();
        if (root == null || !root.TryGetValue<ListTag>(EntitiesKey, out var list)) return records;

        foreach (var child in list)
        {
            if (child is not CompoundTag entry) continue;
            if (!entry.TryGetValue<LongTag>(NBTKey.ENTITY.id, out var id)) continue;
            if (!entry.TryGetValue<StringTag>(NBTKey.ENTITY.name, out var name)) continue;
            if (!EntityTagLocation.TryReadSector(entry, out var sector) || !EntityTagLocation.TryReadLocal(entry, out var local)) continue;

            records.Add(new EntityRecord(id.Value, EntityFiles.NameOf(name.Value), sector, local));
        }
        return records;
    }

    public static bool TryCreateRecord(CompoundTag entity, string fileName, out EntityRecord record, out bool hasSector)
    {
        record = default;
        hasSector = false;
        if (entity == null || !entity.TryGetValue<LongTag>(NBTKey.ENTITY.id, out var id)) return false;
        if (!EntityTagLocation.TryReadLocal(entity, out var local)) return false;

        hasSector = EntityTagLocation.TryReadSector(entity, out var sector);
        record = new EntityRecord(id.Value, EntityFiles.NameOf(fileName), sector, local);
        return true;
    }

    public static void Write(string worldFolder, EntityCatalog catalog)
    {
        var tag = ToTag(catalog.Snapshot());
        AtomicFile.Write(PathOf(worldFolder), temp => NbtFile.Write(temp, tag, FormatOptions.Java, CompressionType.GZip));
    }

    public static EntityCatalog Load(string worldFolder, bool repairFiles)
    {
        var catalog = new EntityCatalog();
        string folder = EntityFiles.FolderOf(worldFolder);
        if (!Directory.Exists(folder)) return catalog;

        if (repairFiles) EntityFiles.RepairDoubleExtensions(folder);

        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(folder, "*" + EntityFiles.Extension))
            files[EntityFiles.NameOf(file)] = file;

        var indexed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in ReadIndex(worldFolder))
        {
            if (!files.ContainsKey(record.Name) || !indexed.Add(record.Name)) continue;
            catalog.Upsert(record);
        }

        int added = 0;
        foreach (var pair in files)
        {
            if (indexed.Contains(pair.Key)) continue;
            if (TryIndexFile(catalog, pair.Key, pair.Value)) added++;
        }

        if (added > 0) Debug.Log($"[EntityCatalog] Indexed {added} entity files that were missing from {FileName}");
        return catalog;
    }

    private static List<EntityRecord> ReadIndex(string worldFolder)
    {
        string path = PathOf(worldFolder);
        if (!File.Exists(path)) return new List<EntityRecord>();

        try
        {
            return FromTag(NbtFile.Read(path, FormatOptions.Java, CompressionType.GZip));
        }
        catch (Exception ex)
        {
            Debug.Warning($"[EntityCatalog] Cannot read {FileName} ({ex.Message}), rebuilding it from the entity files");
            return new List<EntityRecord>();
        }
    }

    private static bool TryIndexFile(EntityCatalog catalog, string name, string file)
    {
        try
        {
            var tag = NbtFile.Read(file, FormatOptions.Java, CompressionType.GZip);
            if (!TryCreateRecord(tag, name, out var record, out bool hasSector))
            {
                Debug.Warning($"[EntityCatalog] Entity file {Path.GetFileName(file)} has no id or position and is ignored");
                return false;
            }

            if (!hasSector) Debug.Warning($"[EntityCatalog] Entity file {Path.GetFileName(file)} has no sector, it is placed in sector {Vector3i.Zero}");

            if (catalog.TryGet(record.Id, out var existing))
            {
                Debug.Warning($"[EntityCatalog] Entity {record.Id} is saved twice, '{name}' is ignored and '{existing.Name}' is used");
                return false;
            }

            catalog.Upsert(record);
            return true;
        }
        catch (Exception ex)
        {
            Debug.Error($"[EntityCatalog] Cannot read entity file {file}: {ex.Message}. A copy is kept as {Path.GetFileName(file)}.corrupt");
            try { File.Copy(file, file + ".corrupt", true); } catch (IOException) { }
            return false;
        }
    }
}
