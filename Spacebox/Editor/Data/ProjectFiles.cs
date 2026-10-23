namespace Spacebox.Editor.Data;

public static class ProjectFiles
{
    public static List<string> List(string root, string folder, params string[] extensions)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(root)) return result;

        string start = string.IsNullOrEmpty(folder) ? root : Path.Combine(root, folder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(start)) return result;

        foreach (var file in Directory.EnumerateFiles(start, "*.*", SearchOption.AllDirectories))
        {
            if (!HasExtension(file, extensions)) continue;
            result.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }
        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public static List<string> Names(string root, string folder, params string[] extensions)
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in List(root, folder, extensions)) names.Add(Path.GetFileNameWithoutExtension(path));
        return names.ToList();
    }

    public static List<string> Union(params IEnumerable<string>[] lists)
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in lists)
        {
            if (list != null) names.UnionWith(list);
        }
        return names.ToList();
    }

    private static bool HasExtension(string file, string[] extensions)
    {
        if (extensions == null || extensions.Length == 0) return true;
        string extension = Path.GetExtension(file);
        foreach (var candidate in extensions)
        {
            if (string.Equals(extension, candidate, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
