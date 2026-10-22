using Engine;
using Spacebox.Game.Generation.Tools.SaveData;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Respawn;
using Spacebox.Game.Protection;
using Spacebox.Game.Rules;

namespace Spacebox.Game.Versioning;

public static class WorldJsonToNbt
{
    private static readonly (string Legacy, string Current)[] Files =
    {
        (WorldFileLayout.LegacyWorldFile, WorldFileLayout.WorldFile),
        (WorldFileLayout.LegacyLocalPlayerFile, WorldFileLayout.LocalPlayerFile),
        (WorldFileLayout.LegacyDropsFile, WorldFileLayout.DropsFile),
        (WorldFileLayout.LegacyTagsFile, WorldFileLayout.TagsFile),
        (WorldRulesStore.LegacyFileName, WorldRulesStore.FileName),
        (ClaimIndex.LegacyFileName, ClaimIndex.FileName),
        (EntityProtection.LegacyFileName, EntityProtection.FileName),
        (ReconstructorRecordsFile.LegacyFileName, ReconstructorRecordsFile.FileName)
    };

    public static int Convert(string worldFolder)
    {
        if (string.IsNullOrEmpty(worldFolder) || !Directory.Exists(worldFolder)) return 0;

        PlayerFiles.MoveLegacyFolder(worldFolder);
        int converted = 0;
        foreach (var (legacy, current) in Files)
        {
            if (ConvertFile(Path.Combine(worldFolder, legacy), Path.Combine(worldFolder, current))) converted++;
        }

        string players = Path.Combine(worldFolder, WorldFileLayout.PlayersFolder);
        if (Directory.Exists(players))
        {
            foreach (var file in Directory.GetFiles(players, "*.json"))
            {
                if (ConvertFile(file, Path.ChangeExtension(file, WorldFileLayout.PlayerExtension))) converted++;
            }
        }

        if (converted > 0) Debug.Log($"[Save] Converted {converted} JSON file(s) to NBT in {Path.GetFileName(worldFolder)}");
        return converted;
    }

    private static bool ConvertFile(string legacyPath, string currentPath)
    {
        if (!File.Exists(legacyPath)) return false;

        try
        {
            if (File.Exists(currentPath))
            {
                File.Delete(legacyPath);
                return false;
            }

            string json = File.ReadAllText(legacyPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                File.Delete(legacyPath);
                return false;
            }

            SaveDataFile.WriteJson(currentPath, json, legacyPath);
            return true;
        }
        catch (Exception ex)
        {
            Debug.Warning($"[Save] Could not convert {legacyPath} to NBT: {ex.Message}");
            return false;
        }
    }
}
