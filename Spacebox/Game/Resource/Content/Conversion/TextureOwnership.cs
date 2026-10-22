using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content.Conversion;

public sealed class TextureOwnership
{
    private readonly Dictionary<string, string> _uniqueImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _references = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public TextureOwnership(string folderRoot, IEnumerable<string> existingEntryDirectories, IReadOnlyList<JsonObject> entries)
    {
        var excluded = new List<string>();
        foreach (var directory in existingEntryDirectories) excluded.Add(Path.GetFullPath(directory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar);

        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(folderRoot))
        {
            foreach (var file in Directory.GetFiles(folderRoot, "*.*", SearchOption.AllDirectories))
            {
                if (!GameSetFormat.HasExtension(file, GameSetFormat.ImageExtensions)) continue;
                if (GameSetFormat.IsIgnoredPath(folderRoot, file) || GameSetFormat.IsInEmissionsFolder(folderRoot, file)) continue;

                string name = AssetFolderScanner.NameOf(file);
                if (_uniqueImages.ContainsKey(name) || duplicates.Contains(name))
                {
                    _uniqueImages.Remove(name);
                    duplicates.Add(name);
                    continue;
                }

                if (IsInside(file, excluded)) duplicates.Add(name);
                else _uniqueImages[name] = file;
            }
        }

        foreach (var entry in entries) Count(entry);
        foreach (var directory in existingEntryDirectories)
        {
            foreach (var json in Directory.GetFiles(directory, "*.json"))
            {
                if (ContentJson.ParseFile(json, null) is JsonObject obj) Count(obj);
            }
        }
    }

    public List<string> OwnedBy(JsonObject entry)
    {
        var owned = new List<string>();
        foreach (var value in Strings(entry))
        {
            if (!_uniqueImages.TryGetValue(value, out var file)) continue;
            if (!_references.TryGetValue(value, out int count) || count != 1) continue;
            if (!owned.Contains(file)) owned.Add(file);
        }
        return owned;
    }

    private void Count(JsonObject entry)
    {
        var names = new HashSet<string>(Strings(entry), StringComparer.OrdinalIgnoreCase);
        foreach (var name in names) _references[name] = _references.TryGetValue(name, out int count) ? count + 1 : 1;
    }

    public static List<string> Strings(JsonNode node)
    {
        var values = new List<string>();
        Collect(node, values);
        return values;
    }

    private static void Collect(JsonNode node, List<string> values)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj) Collect(property.Value, values);
                break;
            case JsonArray array:
                foreach (var item in array) Collect(item, values);
                break;
            case JsonValue value when value.TryGetValue(out string text) && !string.IsNullOrWhiteSpace(text):
                values.Add(text.Trim().ToLowerInvariant());
                break;
        }
    }

    private static bool IsInside(string file, List<string> directories)
    {
        string full = Path.GetFullPath(file);
        foreach (var directory in directories)
        {
            if (full.StartsWith(directory, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
