using Spacebox.Game.Resource.Content;

namespace Spacebox.Network;

public static class GameSetLocator
{
    private const string DefaultFolder = "Default";

    public static ModFolders Local => ModFolders.Local;

    public static string FindFolder(string modId)
    {
        return TryFindFolderPath(Local, modId, out var path) ? Path.GetFileName(path) : DefaultFolder;
    }

    public static string FolderPath(string folderName)
    {
        foreach (var folder in Local.ContentFolders)
        {
            string path = Path.Combine(folder, folderName);
            if (Directory.Exists(path)) return path;
        }
        return Path.Combine(Local.GameSets, folderName);
    }

    public static bool TryFindFolderPath(ModFolders folders, string modId, out string path)
    {
        var gameSet = InstalledGameSets.Find(InstalledGameSets.Scan(folders), modId);
        path = gameSet != null && string.Equals(gameSet.Id, modId, StringComparison.OrdinalIgnoreCase) ? gameSet.Folder : null;
        return path != null;
    }
}
