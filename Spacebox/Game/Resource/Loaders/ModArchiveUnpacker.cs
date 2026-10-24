using Engine;

namespace Spacebox.Game.Resource;

public static class ModArchiveUnpacker
{
    public static void UnpackFolder(string folder, IModArchiveRule rule)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            return;
        }

        foreach (var archivePath in Directory.GetFiles(folder))
        {
            if (ModArchive.IsArchive(archivePath)) Unpack(folder, archivePath, rule);
        }
    }

    public static bool Unpack(string folder, string archivePath, IModArchiveRule rule)
    {
        string tempExtractPath = null;
        try
        {
            tempExtractPath = ModFolderInstaller.CreateTempFolder(folder);

            Debug.Log($"[GameSetsUnpacker] Processing archive: {archivePath}");
            Debug.Log($"[GameSetsUnpacker] Temp extract path: {tempExtractPath}");

            ModArchive.Extract(archivePath, tempExtractPath);

            string content = rule.FindContent(tempExtractPath);
            if (content == null)
            {
                Debug.Error($"[GameSetsUnpacker] No valid mod folder found in {archivePath}");
                return false;
            }

            var target = rule.ChooseTarget(content, archivePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target.Folder));

            Debug.Log($"[GameSetsUnpacker] Mod folder found: {content}");
            Debug.Log($"[GameSetsUnpacker] Target folder: {target.Folder}");
            if (target.Replaces) Debug.Log($"[GameSetsUnpacker] Updating the installed mod in {target.Folder}");

            ModFolderInstaller.Install(content, target.Folder, target.Replaces);

            File.Delete(archivePath);
            Debug.Success($"[GameSetsUnpacker] Successfully processed: {archivePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Error($"[GameSetsUnpacker] Error processing {Path.GetFileName(archivePath)}: {ex.Message}");
            Debug.Error($"[GameSetsUnpacker] Stack trace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            DeleteTempFolder(tempExtractPath);
        }
    }

    private static void DeleteTempFolder(string tempExtractPath)
    {
        try
        {
            ModFolderInstaller.DeleteIfExists(tempExtractPath);
        }
        catch (Exception ex)
        {
            Debug.Error($"[GameSetsUnpacker] Could not delete {tempExtractPath}: {ex.Message}");
        }
    }
}
