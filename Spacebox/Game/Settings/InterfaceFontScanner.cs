namespace Spacebox.Game;

public static class InterfaceFontScanner
{
    public const string FontsFolder = "Resources/Fonts";
    public const int DefaultSize = 12;

    private static readonly string[] Extensions = { ".ttf", ".otf" };

    public static List<InterfaceFont> Scan(string baseDirectory, IReadOnlyList<InterfaceFont> known)
    {
        var found = new List<InterfaceFont>();
        string root = Path.Combine(baseDirectory, FontsFolder);
        if (!Directory.Exists(root)) return found;

        var files = new List<string>(Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories));
        files.Sort(StringComparer.OrdinalIgnoreCase);
        foreach (string file in files)
        {
            if (!IsFontFile(file)) continue;
            string relative = FontsFolder + "/" + Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (Contains(known, relative)) continue;
            found.Add(Describe(relative));
        }
        return found;
    }

    public static InterfaceFont Describe(string relativePath)
    {
        string name = Path.GetFileNameWithoutExtension(relativePath);
        string inFolder = relativePath.Substring(FontsFolder.Length + 1);
        string id = "file:" + Path.ChangeExtension(inFolder, null).ToLowerInvariant();
        bool pixelated = name.Contains("pixel", StringComparison.OrdinalIgnoreCase);
        return new InterfaceFont(id, name, relativePath, DefaultSize, pixelated);
    }

    private static bool IsFontFile(string file)
    {
        string extension = Path.GetExtension(file);
        for (int i = 0; i < Extensions.Length; i++)
        {
            if (extension.Equals(Extensions[i], StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool Contains(IReadOnlyList<InterfaceFont> fonts, string relativePath)
    {
        for (int i = 0; i < fonts.Count; i++)
        {
            if (string.Equals(fonts[i].RelativePath, relativePath, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
