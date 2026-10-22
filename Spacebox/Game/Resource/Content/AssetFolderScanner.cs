namespace Spacebox.Game.Resource.Content;

public static class AssetFolderScanner
{
    public static Dictionary<string, string> Scan(string root, string[] extensions, Func<string, bool> include, bool lastWins)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return result;

        var files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (!GameSetFormat.HasExtension(file, extensions)) continue;
            if (GameSetFormat.IsIgnoredPath(root, file)) continue;
            if (include != null && !include(file)) continue;

            string name = NameOf(file);
            if (lastWins || !result.ContainsKey(name)) result[name] = file;
        }

        return result;
    }

    public static Dictionary<string, string> ScanByExtension(string root, string[] extensions, Func<string, bool> include)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in extensions)
            Fill(result, Scan(root, new[] { extension }, include, false));
        return result;
    }

    public static string NameOf(string file)
    {
        return Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
    }

    public static void Overlay(Dictionary<string, string> target, Dictionary<string, string> layer)
    {
        foreach (var pair in layer) target[pair.Key] = pair.Value;
    }

    public static void Fill(Dictionary<string, string> target, Dictionary<string, string> layer)
    {
        foreach (var pair in layer) target.TryAdd(pair.Key, pair.Value);
    }
}
