namespace Spacebox.Game.Resource;

public static class ModFolderInstaller
{
    public const string TempPrefix = "temp_";
    public const string OldSuffix = ".old";

    public static string CreateTempFolder(string root)
    {
        string path = Path.Combine(root, TempPrefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static bool IsTransient(string folderPath)
    {
        string name = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return name.StartsWith(TempPrefix, StringComparison.OrdinalIgnoreCase) || name.EndsWith(OldSuffix, StringComparison.OrdinalIgnoreCase);
    }

    public static void Install(string source, string target, bool replaces)
    {
        if (!replaces || !Directory.Exists(target))
        {
            Directory.Move(source, target);
            return;
        }

        string old = target + OldSuffix;
        DeleteIfExists(old);
        Directory.Move(target, old);
        try
        {
            Directory.Move(source, target);
        }
        catch
        {
            Directory.Move(old, target);
            throw;
        }

        DeleteIfExists(old);
    }

    public static void DeleteIfExists(string folder)
    {
        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder)) Directory.Delete(folder, true);
    }
}
