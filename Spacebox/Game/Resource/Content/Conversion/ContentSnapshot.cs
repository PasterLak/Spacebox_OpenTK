using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content.Conversion;

public static class ContentSnapshot
{
    public static JsonObject Create(string gameSetRoot)
    {
        var layer = new ContentLayer("gameset", gameSetRoot);
        var content = GameSetContentReader.Read(new[] { layer });
        var assets = GameSetAssetIndex.Build(new[] { layer });
        return Create(content, assets);
    }

    public static JsonObject Create(GameSetContent content, GameSetAssetIndex assets)
    {
        return new JsonObject
        {
            ["Blocks"] = Ordered(content.Blocks),
            ["Items"] = Ordered(content.Items),
            ["Categories"] = Ordered(content.Categories),
            ["Blueprints"] = Keyed(content.Blueprints),
            ["Recipes"] = RecipesByType(content.Recipes),
            ["BlockTextures"] = Hashes(assets.BlockTextures),
            ["ItemTextures"] = Hashes(assets.ItemTextures),
            ["BlockEmissions"] = Hashes(Used(assets.BlockEmissions, assets.BlockTextures)),
            ["ItemEmissions"] = Hashes(Used(assets.ItemEmissions, assets.ItemTextures)),
            ["Sounds"] = Hashes(assets.Sounds)
        };
    }

    public static string ToText(JsonObject snapshot) => ContentJson.Write(snapshot);

    public static List<string> Differences(JsonObject expected, JsonObject actual, int limit = 20)
    {
        var differences = new List<string>();
        foreach (var section in expected)
        {
            var other = actual[section.Key];
            Compare(section.Key, section.Value, other, differences, limit);
        }
        foreach (var section in actual)
        {
            if (!expected.ContainsKey(section.Key)) differences.Add($"{section.Key}: unexpected section");
        }
        return differences;
    }

    private static void Compare(string path, JsonNode expected, JsonNode actual, List<string> differences, int limit)
    {
        if (differences.Count >= limit) return;

        if (expected is JsonObject expectedObject && actual is JsonObject actualObject && path.IndexOf('/') < 0)
        {
            foreach (var pair in expectedObject)
            {
                if (!actualObject.ContainsKey(pair.Key)) differences.Add($"{path}/{pair.Key}: missing");
                else Compare(path + "/" + pair.Key, pair.Value, actualObject[pair.Key], differences, limit);
                if (differences.Count >= limit) return;
            }
            foreach (var pair in actualObject)
            {
                if (!expectedObject.ContainsKey(pair.Key)) differences.Add($"{path}/{pair.Key}: added");
                if (differences.Count >= limit) return;
            }
            return;
        }

        if (expected is JsonArray expectedArray && actual is JsonArray actualArray && path.IndexOf('/') < 0)
        {
            int count = Math.Max(expectedArray.Count, actualArray.Count);
            for (int i = 0; i < count; i++)
            {
                if (i >= expectedArray.Count) differences.Add($"{path}[{i}]: added {Short(actualArray[i])}");
                else if (i >= actualArray.Count) differences.Add($"{path}[{i}]: missing {Short(expectedArray[i])}");
                else if (!JsonNode.DeepEquals(expectedArray[i], actualArray[i])) differences.Add($"{path}[{i}]: {Short(expectedArray[i])} became {Short(actualArray[i])}");
                if (differences.Count >= limit) return;
            }
            return;
        }

        if (!JsonNode.DeepEquals(expected, actual)) differences.Add($"{path}: {Short(expected)} became {Short(actual)}");
    }

    private static string Short(JsonNode node)
    {
        string text = node?.ToJsonString() ?? "null";
        return text.Length > 160 ? text.Substring(0, 160) + "..." : text;
    }

    private static JsonArray Ordered(IReadOnlyList<ContentEntry> entries)
    {
        var array = new JsonArray();
        foreach (var entry in entries) array.Add(new JsonObject { ["Key"] = entry.Key, ["Data"] = entry.Data.DeepClone() });
        return array;
    }

    private static JsonObject Keyed(IReadOnlyList<ContentEntry> entries)
    {
        var sorted = new List<ContentEntry>(entries);
        sorted.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        var result = new JsonObject();
        int unnamed = 0;
        foreach (var entry in sorted)
        {
            string key = string.IsNullOrEmpty(entry.Key) ? "#" + unnamed++ : entry.Key;
            result[key] = entry.Data.DeepClone();
        }
        return result;
    }

    private static JsonObject RecipesByType(IReadOnlyList<ContentEntry> entries)
    {
        var types = new SortedDictionary<string, JsonArray>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            string type = (ContentJson.GetString(entry.Data, "Type") ?? "unknown").ToLowerInvariant();
            if (!types.TryGetValue(type, out var list)) types[type] = list = new JsonArray();
            list.Add(entry.Data.DeepClone());
        }

        var result = new JsonObject();
        foreach (var pair in types) result[pair.Key] = pair.Value;
        return result;
    }

    private static Dictionary<string, string> Used(Dictionary<string, string> emissions, Dictionary<string, string> textures)
    {
        var used = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in emissions)
        {
            if (textures.ContainsKey(pair.Key)) used[pair.Key] = pair.Value;
        }
        return used;
    }

    private static JsonObject Hashes(Dictionary<string, string> files)
    {
        var names = new List<string>(files.Keys);
        names.Sort(StringComparer.Ordinal);
        var result = new JsonObject();
        foreach (var name in names) result[name] = HashFile(files[name]);
        return result;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).Substring(0, 16).ToLowerInvariant();
    }
}
