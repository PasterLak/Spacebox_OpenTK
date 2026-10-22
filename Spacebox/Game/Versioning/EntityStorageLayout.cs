using Engine;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Versioning;

public static class EntityStorageLayout
{
    public static readonly string[] ReconstructorIds = { "default:dna_reconstructor" };

    public static bool HasLegacyFiles(string worldFolder)
    {
        string sectors = Path.Combine(worldFolder ?? "", WorldFileLayout.SectorsFolder);
        return Directory.Exists(sectors) && Directory.EnumerateFiles(sectors, "*" + EntityFiles.Extension, SearchOption.AllDirectories).Any();
    }

    public static void Upgrade(WorldUpgradeContext world)
    {
        int moved = EntityFolderMigration.Apply(world);
        int reconstructors = ReconstructorSeeder.Apply(world, ReconstructorIds);
        world.Write($"Moved {moved} entity files to {EntityFiles.FolderName}/, registered {reconstructors} DNA reconstructors");
    }

    public const string BackupLabel = "entity_layout";

    public static string UpgradeInPlace(string worldFolder, string backupDirectory = null)
    {
        if (!HasLegacyFiles(worldFolder)) return null;

        string backup = null;
        try
        {
            backup = WorldBackup.Create(worldFolder, BackupLabel, backupDirectory, DateTime.Now);
            Debug.Log($"[EntityStorageLayout] Backup before moving the entity files: {backup}");
        }
        catch (Exception ex)
        {
            Debug.Error($"[EntityStorageLayout] Could not create a backup, moving the entity files without one: {ex.Message}");
        }

        var world = new WorldUpgradeContext(new WorldInfo { Name = Path.GetFileName(worldFolder) }, worldFolder);
        try
        {
            Upgrade(world);
        }
        catch (Exception ex)
        {
            string restoreError = new WorldFolderRestorer().Restore(backup, worldFolder);
            Debug.Error($"[EntityStorageLayout] Moving the entity files failed: {ex.Message}" + (restoreError == null ? "" : $". Restoring the backup failed: {restoreError}"));
            throw;
        }

        foreach (var line in world.Log) Debug.Warning("[EntityStorageLayout] " + line);
        return backup;
    }
}
