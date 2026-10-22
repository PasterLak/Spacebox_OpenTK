using Spacebox.Game.Generation.Tools.SaveData;
using System.Text.Json;
using Spacebox.Game.GUI;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.ShipViewer;

public static class SavedWorldCatalog
{
    private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static List<SavedWorld> Scan(string worldsDirectory, WorldUpgradeRegistry registry, string currentVersion)
    {
        var worlds = new List<SavedWorld>();
        if (string.IsNullOrEmpty(worldsDirectory) || !Directory.Exists(worldsDirectory)) return worlds;

        foreach (var folder in Directory.GetDirectories(worldsDirectory))
        {
            if (string.Equals(Path.GetFileName(folder), WorldUpgrader.BackupFolderName, StringComparison.OrdinalIgnoreCase)) continue;

            if (!WorldInfoFiles.Exists(folder)) continue;

            worlds.Add(Read(folder, registry, currentVersion));
        }

        worlds.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return worlds;
    }

    public static SavedWorld Read(string folder, WorldUpgradeRegistry registry, string currentVersion)
    {
        WorldInfo info;
        try
        {
            info = SaveDataFile.Read<WorldInfo>(WorldInfoFiles.PathIn(folder), WorldInfoFiles.LegacyPathIn(folder), ReadOptions);
        }
        catch (Exception ex)
        {
            return new SavedWorld(folder, null, SavedWorldStatus.Corrupt, $"The world file could not be read: {ex.Message}");
        }

        if (info == null) return new SavedWorld(folder, null, SavedWorldStatus.Corrupt, "The world file is empty or damaged");

        var status = registry.Check(info.GameVersion ?? "", currentVersion, new List<IWorldUpgradeStep>(), out var error);
        return status switch
        {
            WorldUpgradeStatus.UpToDate => new SavedWorld(folder, info, SavedWorldStatus.Ready, null),
            WorldUpgradeStatus.Ok => new SavedWorld(folder, info, SavedWorldStatus.NeedsUpgrade, $"Saved with {info.GameVersion}, converted in memory for viewing"),
            _ => new SavedWorld(folder, info, SavedWorldStatus.Unsupported, error ?? $"Version {info.GameVersion} is not supported")
        };
    }
}
