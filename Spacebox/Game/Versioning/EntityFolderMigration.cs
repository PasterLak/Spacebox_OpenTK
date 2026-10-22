using SharpNBT;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Versioning;

public static class EntityFolderMigration
{
    public static int Apply(WorldUpgradeContext world)
    {
        if (!world.HasFolder) return 0;

        string sectorsRoot = Path.Combine(world.WorldFolder, WorldFileLayout.SectorsFolder);
        string entitiesFolder = EntityFiles.FolderOf(world.WorldFolder);
        var catalog = EntityCatalogFile.Load(world.WorldFolder, true);
        int moved = 0;

        if (Directory.Exists(sectorsRoot))
        {
            foreach (var sectorFolder in Directory.GetDirectories(sectorsRoot))
            {
                if (!SectorFolderName.TryParse(Path.GetFileName(sectorFolder), out var sectorIndex))
                {
                    world.Write($"Skipped folder {Path.GetFileName(sectorFolder)}: not a sector folder");
                    continue;
                }

                EntityFiles.RepairDoubleExtensions(sectorFolder);
                foreach (var file in Directory.GetFiles(sectorFolder, "*" + EntityFiles.Extension))
                {
                    if (MoveEntity(world, catalog, file, sectorIndex, entitiesFolder)) moved++;
                }
            }
        }

        EntityCatalogFile.Write(world.WorldFolder, catalog);
        world.Count("entity files moved", moved);
        return moved;
    }

    private static bool MoveEntity(WorldUpgradeContext world, EntityCatalog catalog, string file, OpenTK.Mathematics.Vector3i sectorIndex, string entitiesFolder)
    {
        string name = EntityFiles.NameOf(file);
        CompoundTag tag;
        try
        {
            tag = NbtFileEditor.Read(file);
        }
        catch (Exception ex)
        {
            world.Write($"Skipped unreadable entity file {Path.GetFileName(file)}: {ex.Message}");
            return false;
        }

        if (!EntityTagLocation.TryReadLocal(tag, out var local) || !tag.TryGetValue<LongTag>(NBTKey.ENTITY.id, out var id))
        {
            world.Write($"Skipped entity file {Path.GetFileName(file)}: no id or position");
            return false;
        }

        string target = EntityFiles.PathOf(entitiesFolder, name);
        if (File.Exists(target) && File.GetLastWriteTimeUtc(target) >= File.GetLastWriteTimeUtc(file))
        {
            world.Write($"Entity {name} is saved in more than one sector, the newer file is kept");
            File.Delete(file);
            return false;
        }

        if (catalog.TryGet(id.Value, out var existing) && existing.Name != name)
        {
            world.Write($"Entity {id.Value} is saved as '{existing.Name}' and '{name}', '{existing.Name}' is kept");
            File.Delete(file);
            return false;
        }

        var savedAt = File.GetLastWriteTimeUtc(file);
        EntityTagLocation.Write(tag, sectorIndex, local);
        Directory.CreateDirectory(entitiesFolder);
        NbtFileEditor.Write(target, tag);
        File.SetLastWriteTimeUtc(target, savedAt);
        File.Delete(file);
        catalog.Upsert(new EntityRecord(id.Value, name, sectorIndex, local));
        return true;
    }
}
