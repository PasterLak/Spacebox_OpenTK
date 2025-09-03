using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.InputPro;

public class InputBindingConverter : JsonConverter<InputBinding>
{
    public override InputBinding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("Type", out var typeElement))
            return null;

        var type = typeElement.GetString();
        return type switch
        {
            "Key" => new KeyBinding
            {
                Key = Enum.Parse<Keys>(root.GetProperty("Key").GetString())
            },
            "MouseKey" => new MouseKeyBinding
            {
                Key = Enum.Parse<MouseButton>(root.GetProperty("Key").GetString())
            },
            "Composite" => new CompositeBinding
            {
                RequireAll = root.GetProperty("RequireAll").GetBoolean(),
                Bindings = root.GetProperty("Bindings").EnumerateArray()
                    .Select(e =>
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(e.GetRawText());
                        var reader = new Utf8JsonReader(bytes);
                        return Read(ref reader, typeToConvert, options);
                    })
                    .ToList()
            },
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, InputBinding value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case KeyBinding kb:
                writer.WriteString("Type", "Key");
                writer.WriteString("Key", kb.Key.ToString());
                break;
            case MouseKeyBinding mb:
                writer.WriteString("Type", "MouseKey");
                writer.WriteString("Key", mb.Key.ToString());
                break;
            case CompositeBinding cb:
                writer.WriteString("Type", "Composite");
                writer.WriteBoolean("RequireAll", cb.RequireAll);
                writer.WriteStartArray("Bindings");
                foreach (var binding in cb.Bindings)
                {
                    Write(writer, binding, options);
                }
                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }
}