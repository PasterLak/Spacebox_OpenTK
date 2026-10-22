using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Utils;

namespace Spacebox.Game.Resource.Content;

public static class ContentJson
{
    public static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static readonly JsonSerializerOptions CaseInsensitive = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static readonly JsonSerializerOptions Indented = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonNode ParseFile(string path, ICollection<string> warnings)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            warnings?.Add($"Cannot read '{path}': {ex.Message}");
            return null;
        }

        return Parse(text, path, warnings);
    }

    public static JsonNode Parse(string text, string source, ICollection<string> warnings)
    {
        try
        {
            return JsonNode.Parse(text, documentOptions: DocumentOptions);
        }
        catch (JsonException)
        {
            try
            {
                var node = JsonNode.Parse(JsonFixer.FixJson(text), documentOptions: DocumentOptions);
                warnings?.Add($"JSON syntax errors fixed in '{source}'");
                return node;
            }
            catch (JsonException ex)
            {
                warnings?.Add($"Invalid JSON in '{source}': {ex.Message}");
                return null;
            }
        }
    }

    public static JsonNode Get(JsonObject obj, string key)
    {
        if (obj == null) return null;
        if (obj.TryGetPropertyValue(key, out var exact)) return exact;

        foreach (var property in obj)
        {
            if (string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase)) return property.Value;
        }
        return null;
    }

    public static string GetString(JsonObject obj, string key)
    {
        return Get(obj, key) is JsonValue value && value.TryGetValue(out string text) ? text : null;
    }

    public static void Set(JsonObject obj, string key, JsonNode value)
    {
        foreach (var property in obj)
        {
            if (string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                obj[property.Key] = value;
                return;
            }
        }
        obj[key] = value;
    }

    public static JsonObject Merge(JsonObject target, JsonObject patch)
    {
        var result = (JsonObject)target.DeepClone();
        MergeInto(result, patch);
        return result;
    }

    private static void MergeInto(JsonObject target, JsonObject patch)
    {
        foreach (var property in patch)
        {
            var existing = Get(target, property.Key);
            if (existing is JsonObject existingObject && property.Value is JsonObject patchObject)
            {
                MergeInto(existingObject, patchObject);
                continue;
            }
            Set(target, property.Key, property.Value?.DeepClone());
        }
    }

    public static string Write(JsonNode node)
    {
        return node.ToJsonString(Indented);
    }
}
