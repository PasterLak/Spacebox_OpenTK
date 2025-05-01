using System.IO.Compression;
using System.Text.Json;
using Engine;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

namespace Spacebox.Game.Resource
{
    internal static class GameSetsUnpacker
    {
        public static void UnpackMods(bool useModNameAsFolder)
        {
            string gameSetsPath = Path.Combine(Globals.GameFolder, Globals.GameSet.LocalFolder);

            if (!Directory.Exists(gameSetsPath))
            {
                Directory.CreateDirectory(gameSetsPath);
                return;
            }

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
                    string targetFolderName = !string.IsNullOrEmpty(modName) ? modName : Path.GetFileNameWithoutExtension(archivePath);
                    string targetFolder = Path.Combine(gameSetsPath, targetFolderName);

                    Debug.Log($"[GameSetsUnpacker] Mod folder found: {modFolder}");
                    Debug.Log($"[GameSetsUnpacker] Target folder: {targetFolder}");

                    if (Directory.Exists(targetFolder))
                    {
                        Debug.Log($"[GameSetsUnpacker] Removing existing folder: {targetFolder}");
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

}