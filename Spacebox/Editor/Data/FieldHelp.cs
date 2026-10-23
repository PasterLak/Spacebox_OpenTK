using System.Reflection;
using System.Text.Json.Nodes;

namespace Spacebox.Editor.Data;

public sealed class FieldHelp
{
    public const string RelativePath = "Resources/Editor/field-help.json";
    public const string BlockTypes = "BlockTypes";
    public const string BlockComponents = "BlockComponents";
    public const string ItemTypes = "ItemTypes";
    public const string MobBehaviours = "MobBehaviours";
    public const string TutorialTasks = "TutorialTasks";

    private static FieldHelp _default;
    private readonly Dictionary<string, Dictionary<string, string>> _texts;

    public FieldHelp(Dictionary<string, Dictionary<string, string>> texts)
    {
        _texts = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in texts) _texts[pair.Key] = new Dictionary<string, string>(pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    public static FieldHelp Default => _default ??= Load(Path.Combine(AppContext.BaseDirectory, RelativePath));

    public static FieldHelp Load(string path)
    {
        var texts = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return new FieldHelp(texts);

        try
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root) return new FieldHelp(texts);

            foreach (var type in root)
            {
                if (type.Value is not JsonObject properties) continue;

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in properties)
                {
                    if (property.Value is JsonValue value && value.TryGetValue(out string text)) map[property.Key] = text;
                }
                texts[type.Key] = map;
            }
        }
        catch (Exception ex)
        {
            Engine.Debug.Warning($"[Editor] Could not read {path}: {ex.Message}");
        }

        return new FieldHelp(texts);
    }

    public string For(Type owner, string property)
    {
        if (owner == null || string.IsNullOrEmpty(property)) return null;

        var declaring = owner.GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.DeclaringType ?? owner;
        for (var type = declaring; type != null && type != typeof(object); type = type.BaseType)
        {
            if (_texts.TryGetValue(type.Name, out var map) && map.TryGetValue(property, out var text)) return text;
        }

        return null;
    }

    public string Text(string group, string key)
    {
        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(key)) return null;
        return _texts.TryGetValue(group, out var map) && map.TryGetValue(key, out var text) ? text : null;
    }

    public bool Has(Type declaringType, string property)
    {
        return _texts.TryGetValue(declaringType.Name, out var map) && map.ContainsKey(property);
    }
}
