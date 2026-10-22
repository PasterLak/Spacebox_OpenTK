using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public static class FieldHelpText
{
    public static string For(Type owner, SchemaProperty property, string key)
    {
        string text = FieldHelp.Default.For(owner, property?.Name ?? key);
        if (property == null) return text;

        string details = $"{FriendlyType(property.ClrType)}, default {DefaultText(property.Default)}";
        return string.IsNullOrEmpty(text) ? details : text + "\n\n" + details;
    }

    private static string DefaultText(JsonNode value)
    {
        if (value == null) return "empty";
        string json = value.ToJsonString();
        return json.Length > 60 ? json.Substring(0, 57) + "..." : json;
    }

    private static string FriendlyType(Type type)
    {
        if (type == null) return "?";
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (JsonSchema.IsDictionary(type)) return "map";
        if (JsonSchema.ElementType(type) != null && type != typeof(string)) return "list";
        return type.Name.ToLowerInvariant();
    }
}
