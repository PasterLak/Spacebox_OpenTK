using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public sealed class SoundUsageIndex
{
    private readonly Dictionary<string, List<string>> _usages = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    public static SoundUsageIndex Build(IEnumerable<(string File, JsonNode Root)> files, ICollection<string> soundNames)
    {
        var index = new SoundUsageIndex();
        var names = new HashSet<string>(soundNames, StringComparer.OrdinalIgnoreCase);
        foreach (var (file, root) in files) index.Collect(file, root, names);
        return index;
    }

    public IReadOnlyList<string> UsagesOf(string sound)
    {
        return sound != null && _usages.TryGetValue(sound, out var list) ? list : Array.Empty<string>();
    }

    private void Collect(string file, JsonNode node, HashSet<string> names)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var pair in obj) Collect(file, pair.Value, names);
                break;
            case JsonArray array:
                foreach (var item in array) Collect(file, item, names);
                break;
            case JsonValue value when value.TryGetValue(out string text) && names.Contains(text):
                if (!_usages.TryGetValue(text, out var list)) _usages[text] = list = new List<string>();
                if (!list.Contains(file)) list.Add(file);
                break;
        }
    }
}
