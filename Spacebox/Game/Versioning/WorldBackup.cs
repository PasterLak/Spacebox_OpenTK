using Engine.Utils;

namespace Spacebox.Game.Versioning;

public static class WorldBackup
{
    public const string FolderName = "_backups";

    public static string DefaultDirectory(string worldFolder)
    {
        return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(worldFolder)) ?? "", FolderName);
    }

    public static string Create(string worldFolder, string label, string backupDirectory, DateTime time)
    {
        string directory = string.IsNullOrEmpty(backupDirectory) ? DefaultDirectory(worldFolder) : backupDirectory;
        Directory.CreateDirectory(directory);

        string name = $"{Path.GetFileName(Path.GetFullPath(worldFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}_{label}_{time:yyyyMMdd_HHmmss}.zip";
        string path = Path.Combine(directory, name);
        File.WriteAllBytes(path, DirectoryArchive.Pack(new[] { new ArchiveFolder(worldFolder, WorldFolderRestorer.ArchiveRoot) }));
        return path;
    }
}
