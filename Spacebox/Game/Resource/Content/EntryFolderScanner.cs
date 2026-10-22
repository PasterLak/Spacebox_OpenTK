namespace Spacebox.Game.Resource.Content;

public static class EntryFolderScanner
{
    public static List<string> FindEntryFiles(string root, string entryFileName)
    {
        var result = new List<string>();
        if (!Directory.Exists(root)) return result;

        Collect(root, entryFileName, result);
        result.Sort((a, b) => string.Compare(Path.GetRelativePath(root, a).Replace('\\', '/'), Path.GetRelativePath(root, b).Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static void Collect(string directory, string entryFileName, List<string> result)
    {
        foreach (var child in Directory.GetDirectories(directory))
        {
            string name = Path.GetFileName(child);
            if (name.StartsWith('_') || string.Equals(name, GameSetFormat.EmissionsFolder, StringComparison.OrdinalIgnoreCase)) continue;

            string entryFile = Path.Combine(child, entryFileName);
            if (File.Exists(entryFile))
            {
                result.Add(entryFile);
                continue;
            }

            Collect(child, entryFileName, result);
        }
    }

    public static List<string> ReadOrder(string root, ICollection<string> warnings)
    {
        var order = new List<string>();
        string path = Path.Combine(root, GameSetFormat.OrderFile);
        if (!File.Exists(path)) return order;

        if (ContentJson.ParseFile(path, warnings) is not System.Text.Json.Nodes.JsonArray array)
        {
            warnings?.Add($"'{path}' must be an array of ids");
            return order;
        }

        foreach (var node in array)
        {
            if (node is System.Text.Json.Nodes.JsonValue value && value.TryGetValue(out string id) && !string.IsNullOrWhiteSpace(id)) order.Add(id);
        }
        return order;
    }

    public static List<T> ApplyOrder<T>(List<T> entries, Func<T, string> idOf, IReadOnlyList<string> order)
    {
        if (order.Count == 0) return entries;

        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < order.Count; i++) rank.TryAdd(order[i], i);

        var ranked = new List<(T Entry, int Rank, int Index)>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            string id = idOf(entries[i]) ?? "";
            ranked.Add((entries[i], rank.TryGetValue(id, out int r) ? r : int.MaxValue, i));
        }

        ranked.Sort((a, b) => a.Rank != b.Rank ? a.Rank.CompareTo(b.Rank) : a.Index.CompareTo(b.Index));

        var result = new List<T>(entries.Count);
        foreach (var item in ranked) result.Add(item.Entry);
        return result;
    }
}
