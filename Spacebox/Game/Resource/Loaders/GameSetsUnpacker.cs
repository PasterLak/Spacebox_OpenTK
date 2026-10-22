using System.IO.Compression;
using System.Text.Json;
using Engine;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource;

internal static class GameSetsUnpacker
{
    public static void UnpackMods(bool useModNameAsFolder)
    {
        var folders = ModFolders.Local;
        foreach (var folder in folders.ContentFolders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                continue;
            }

            UnpackArchives(folder, folders, useModNameAsFolder);
        }
    }

    private static void UnpackArchives(string gameSetsPath, ModFolders folders, bool useModNameAsFolder)
    {
        var archiveFiles = Directory.GetFiles(gameSetsPath)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext == ".zip" || ext == ".rar";
            })
            .ToList();

        if (archiveFiles.Count == 0)
        {
            return;
        }

        foreach (var archivePath in archiveFiles)
        {
            try
            {

                if (!File.Exists(archivePath))
                {
                    Debug.Error($"[GameSetsUnpacker] Archive file not found: {archivePath}");
                    continue;
                }

                string tempExtractPath = Path.Combine(gameSetsPath, $"temp_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempExtractPath);

                Debug.Log($"[GameSetsUnpacker] Processing archive: {archivePath}");
                Debug.Log($"[GameSetsUnpacker] Temp extract path: {tempExtractPath}");

                if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(archivePath, tempExtractPath);
                }
                else if (archivePath.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
                {
                    using var archive = RarArchive.Open(archivePath);
                    foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                    {
                        string destinationPath = Path.Combine(tempExtractPath, entry.Key);
                        if (!ModInstallTarget.IsInside(tempExtractPath, destinationPath))
                            throw new InvalidDataException($"Archive entry '{entry.Key}' is outside the mod folder");
                        string directoryPath = Path.GetDirectoryName(destinationPath);

                        if (!Directory.Exists(directoryPath))
                            Directory.CreateDirectory(directoryPath);

                        entry.WriteToFile(destinationPath, new ExtractionOptions()
                        {
                            Overwrite = true
                        });
                    }
                }

                string modFolder = FindModFolder(tempExtractPath);
                if (modFolder == null)
                {
                    Debug.Error($"[GameSetsUnpacker] No valid mod folder found in {archivePath}");
                    Directory.Delete(tempExtractPath, true);
                    continue;
                }

                string modName = useModNameAsFolder ? GetModNameFromConfig(modFolder) : null;
                string targetFolderName = ModInstallTarget.SafeFolderName(modName, Path.GetFileNameWithoutExtension(archivePath));
                var config = InstalledGameSets.ReadConfig(modFolder);
                bool isExtension = !string.IsNullOrWhiteSpace(config?.Extends);
                string targetRoot = folders.FolderFor(isExtension);
                Directory.CreateDirectory(targetRoot);
                string targetFolder = ModInstallTarget.Resolve(targetRoot, targetFolderName, config?.ModId, folder => InstalledGameSets.ReadConfig(folder)?.ModId, out bool replaces);

                Debug.Log($"[GameSetsUnpacker] Mod folder found: {modFolder}");
                Debug.Log($"[GameSetsUnpacker] Target folder: {targetFolder}");

                if (replaces)
                {
                    Debug.Log($"[GameSetsUnpacker] Updating the installed mod in {targetFolder}");
                    Directory.Delete(targetFolder, true);
                }

                if (!modFolder.Equals(tempExtractPath, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Move(modFolder, targetFolder);
                    Directory.Delete(tempExtractPath, true);
                }
                else
                {
                    Directory.Move(tempExtractPath, targetFolder);
                }

                File.Delete(archivePath);
                Debug.Success($"[GameSetsUnpacker] Successfully processed: {archivePath}");
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetsUnpacker] Error processing {Path.GetFileName(archivePath)}: {ex.Message}");
                Debug.Error($"[GameSetsUnpacker] Stack trace: {ex.StackTrace}");
            }
        }
    }

    private static string FindModFolder(string directory)
    {
        if (File.Exists(Path.Combine(directory, "config.json")))
            return directory;

        foreach (var subDir in Directory.GetDirectories(directory))
        {
            if (File.Exists(Path.Combine(subDir, "config.json")))
                return subDir;
        }

        return null;
    }

    private static string GetModNameFromConfig(string modFolder)
    {
        try
        {
            string configPath = Path.Combine(modFolder, "config.json");
            if (!File.Exists(configPath)) return null;

            var config = JsonSerializer.Deserialize<GameSetLoader.ModConfig>(File.ReadAllText(configPath));
            return config?.ModName?.Trim();
        }
        catch
        {
            return null;
        }
    }
}