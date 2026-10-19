using System.Text.Json.Nodes;

namespace Spacebox.Editor.UI.Fields;

public sealed class FieldContext
{
    public EditorContext Editor { get; set; }
    public string Scope { get; set; } = EditorScopes.Generic;
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public JsonNode Value { get; set; }
    public Type SchemaType { get; set; }
    public JsonObject Parent { get; set; }

    public bool PathContains(string segment)
    {
        return Path.IndexOf(segment, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public bool NameIs(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (string.Equals(Name, names[i], StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public string StringValue => Value is JsonValue value && value.TryGetValue(out string text) ? text : null;
}

public static class EditorScopes
{
    public const string Generic = "generic";
    public const string Blocks = "blocks";
    public const string Items = "items";
    public const string Projectiles = "projectiles";
    public const string Loot = "loot";
    public const string Mobs = "mobs";
    public const string Asteroids = "asteroids";
    public const string Biomes = "biomes";
    public const string Crafting = "crafting";
    public const string Recipes = "recipes";
    public const string Config = "config";
    public const string Generator = "generator";
}

public interface IJsonFieldEditor
{
    bool CanEdit(FieldContext context);
    bool Draw(FieldContext context, out JsonNode value);
}
