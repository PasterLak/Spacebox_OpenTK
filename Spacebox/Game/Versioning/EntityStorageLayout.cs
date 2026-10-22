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

    public static void UpgradeInPlace(string worldFolder)
    {
        if (!HasLegacyFiles(worldFolder)) return;

        var world = new WorldUpgradeContext(new WorldInfo { Name = Path.GetFileName(worldFolder) }, worldFolder);
        Upgrade(world);
        foreach (var line in world.Log) Debug.Warning("[EntityStorageLayout] " + line);
    }
}
