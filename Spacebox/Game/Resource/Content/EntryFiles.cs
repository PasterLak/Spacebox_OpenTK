namespace Spacebox.Game.Resource.Content;

public static class EntryFiles
{
    private static readonly string[] ReservedNames = { GameSetFormat.BlueprintFile, GameSetFormat.EntryRecipesFile, GameSetFormat.OrderFile };

    public static string NamedFile(string folderName) => folderName + ".json";

    public static string Find(string directory, string defaultFileName)
    {
        string named = NamedPath(directory);
        if (named != null && File.Exists(named)) return named;

        string fallback = Path.Combine(directory, defaultFileName);
        return File.Exists(fallback) ? fallback : null;
    }

    public static string PathFor(string directory, string defaultFileName)
    {
        return Find(directory, defaultFileName) ?? NamedPath(directory) ?? Path.Combine(directory, defaultFileName);
    }

    public static bool IsEntryFile(string path, string defaultFileName, Func<string, bool> exists)
    {
        path = path.Replace('\\', '/');
        int slash = path.LastIndexOf('/');
        if (slash <= 0) return false;

        string fileName = path.Substring(slash + 1);
        string directory = path.Substring(0, slash);
        string folderName = directory.Substring(directory.LastIndexOf('/') + 1);

        if (IsNamedFile(fileName, folderName)) return true;
        if (!string.Equals(fileName, defaultFileName, StringComparison.OrdinalIgnoreCase)) return false;
        return IsReserved(NamedFile(folderName)) || !exists(directory + "/" + NamedFile(folderName));
    }

    private static string NamedPath(string directory)
    {
        string folderName = Path.GetFileName(directory.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(folderName)) return null;

        string fileName = NamedFile(folderName);
        return IsReserved(fileName) ? null : Path.Combine(directory, fileName);
    }

    private static bool IsNamedFile(string fileName, string folderName)
    {
        return folderName.Length > 0 && string.Equals(fileName, NamedFile(folderName), StringComparison.OrdinalIgnoreCase) && !IsReserved(fileName);
    }

    private static bool IsReserved(string fileName)
    {
        foreach (var reserved in ReservedNames)
        {
            if (string.Equals(fileName, reserved, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
