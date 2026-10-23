namespace Spacebox.Game.Resource;

public static class ModInstallTarget
{
    public static string SafeFolderName(string name, string fallback)
    {
        string cleaned = Clean(name);
        if (cleaned.Length == 0) cleaned = Clean(fallback);
        if (cleaned.Length == 0) cleaned = "Mod";
        if (string.Equals(cleaned, Globals.GameSet.Default, StringComparison.OrdinalIgnoreCase) || string.Equals(cleaned, Globals.GameSet.Empty, StringComparison.OrdinalIgnoreCase)) cleaned += "_mod";
        return cleaned;
    }

    public static string Resolve(string root, string folderName, string modId, Func<string, string> modIdOf, out bool replaces)
    {
        replaces = false;
        string path = Path.Combine(root, folderName);
        if (!Directory.Exists(path)) return path;

        if (!string.IsNullOrWhiteSpace(modId) && string.Equals(modIdOf(path), modId, StringComparison.OrdinalIgnoreCase))
        {
            replaces = true;
            return path;
        }

        for (int i = 2; ; i++)
        {
            string candidate = Path.Combine(root, $"{folderName}_{i}");
            if (!Directory.Exists(candidate)) return candidate;
        }
    }

    public static bool IsInside(string root, string path)
    {
        string full = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(full, OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static string Clean(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Trim().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalid, chars[i]) >= 0 || chars[i] == '/' || chars[i] == '\\' || chars[i] == ':') chars[i] = '_';
        }

        string cleaned = new string(chars).Trim(' ', '.');
        return cleaned == ".." || cleaned == "." ? "" : cleaned;
    }
}
