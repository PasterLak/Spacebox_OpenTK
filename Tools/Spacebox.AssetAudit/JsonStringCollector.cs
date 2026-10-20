using System.Text.Json;

namespace Spacebox.AssetAudit;

public static class JsonStringCollector
{
    private static readonly JsonDocumentOptions Options = new JsonDocumentOptions
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static List<string> Collect(string json, out string error)
    {
        var values = new List<string>();
        error = null;
        if (string.IsNullOrWhiteSpace(json)) return values;

        try
        {
            using var document = JsonDocument.Parse(json, Options);
            Walk(document.RootElement, values);
        }
        catch (JsonException exception)
        {
            error = exception.Message;
        }
        return values;
    }

    private static void Walk(JsonElement element, List<string> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject()) Walk(property.Value, values);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) Walk(item, values);
                break;
            case JsonValueKind.String:
                values.Add(element.GetString());
                break;
        }
    }
}
