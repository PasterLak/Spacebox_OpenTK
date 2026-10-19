using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public static class EditorJson
{
    public static string FindKey(JsonObject obj, string name)
    {
        if (obj == null || string.IsNullOrEmpty(name)) return null;
        if (obj.ContainsKey(name)) return name;

        foreach (var pair in obj)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)) return pair.Key;
        }

        return null;
    }

    public static JsonNode Get(JsonObject obj, string name)
    {
        var key = FindKey(obj, name);
        return key == null ? null : obj[key];
    }

    public static string GetString(JsonObject obj, string name, string fallback = "")
    {
        return Get(obj, name) is JsonValue value && value.TryGetValue(out string text) ? text : fallback;
    }

    public static double GetNumber(JsonObject obj, string name, double fallback = 0)
    {
        return Get(obj, name) is JsonValue value && TryGetNumber(value, out var number) ? number : fallback;
    }

    public static bool TryGetNumber(JsonValue value, out double number)
    {
        number = 0;
        if (value == null) return false;
        if (value.GetValueKind() != JsonValueKind.Number) return false;
        return value.TryGetValue(out number) || double.TryParse(value.ToJsonString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number);
    }

    public static bool IsIntegral(JsonValue value)
    {
        var text = value.ToJsonString();
        return text.IndexOfAny(new[] { '.', 'e', 'E' }) < 0;
    }

    public static void Set(JsonObject obj, string name, JsonNode value)
    {
        var key = FindKey(obj, name) ?? name;
        obj[key] = value;
    }

    public static bool Remove(JsonObject obj, string name)
    {
        var key = FindKey(obj, name);
        return key != null && obj.Remove(key);
    }

    public static JsonNode Clone(JsonNode node)
    {
        return node?.DeepClone();
    }

    public static bool HasKeys(JsonNode node, params string[] keys)
    {
        if (node is not JsonObject obj || obj.Count != keys.Length) return false;
        for (int i = 0; i < keys.Length; i++)
        {
            if (FindKey(obj, keys[i]) == null) return false;
        }
        return true;
    }

    public static JsonArray GetArray(JsonNode root, string key)
    {
        if (string.IsNullOrEmpty(key)) return root as JsonArray;
        return root is JsonObject obj ? Get(obj, key) as JsonArray : null;
    }

    public static string MakeUniqueId(string baseId, Func<string, bool> exists)
    {
        if (!exists(baseId)) return baseId;

        int counter = 2;
        while (exists(baseId + "_" + counter)) counter++;
        return baseId + "_" + counter;
    }
}
