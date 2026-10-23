using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spacebox.Game.Resource.Content;

public sealed class StringOrListConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return "";
        if (reader.TokenType != JsonTokenType.StartArray) return reader.TokenType == JsonTokenType.String ? reader.GetString() : JsonDocument.ParseValue(ref reader).RootElement.ToString();

        var parts = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String) parts.Add(reader.GetString());
        }
        return string.Join(", ", parts);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        var targets = ExtensionTargets.Parse(value);
        if (targets.Count <= 1)
        {
            writer.WriteStringValue(value ?? "");
            return;
        }

        writer.WriteStartArray();
        foreach (var target in targets) writer.WriteStringValue(target);
        writer.WriteEndArray();
    }
}
