using System.Text.Json.Nodes;
using Spacebox.Editor.Data;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Editor.Canvas.Inspector;

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
    public const string Disassembly = "disassembly";
    public const string PlayerSpawn = "player_spawn";
    public const string GameModes = "game_modes";
    public const string Tutorials = "tutorials";
    public const string Config = "config";
    public const string Generator = "generator";

    public static string ForPath(string path)
    {
        string fileName = System.IO.Path.GetFileName(path);
        if (fileName.Equals(GameSetFormat.BlueprintFile, StringComparison.OrdinalIgnoreCase)) return Crafting;
        if (fileName.Equals(GameSetFormat.EntryRecipesFile, StringComparison.OrdinalIgnoreCase) && path.Contains('/')) return Recipes;
        foreach (var (prefix, scope) in PathScopes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return scope;
        }
        return Generic;
    }

    private static readonly (string Prefix, string Scope)[] PathScopes =
    {
        (GameSetCatalog.BlocksFile, Blocks),
        (GameSetFormat.BlocksFolder + "/", Blocks),
        (GameSetCatalog.ItemsFile, Items),
        (GameSetFormat.ItemsFolder + "/", Items),
        (GameSetCatalog.CraftingFile, Crafting),
        (GameSetFormat.CraftingFolder + "/", Crafting),
        (GameSetFormat.RecipesFolder + "/", Recipes),
        (GameSetCatalog.RecipesFile, Recipes),
        (GameSetCatalog.ConfigFile, Config),
        (GameSetCatalog.ProjectilesFolder + "/", Projectiles),
        (GameSetCatalog.LootFolder + "/", Loot),
        (GameSetCatalog.MobsFolder + "/", Mobs),
        (GameSetCatalog.AsteroidsFolder + "/", Asteroids),
        (GameSetCatalog.BiomesFolder + "/", Biomes),
        (GameSetCatalog.GeneratorFile, Generator),
        (GameSetCatalog.GameModesFolder + "/", GameModes),
        (GameSetCatalog.DisassemblyFile, Disassembly),
        (GameSetCatalog.TutorialsFolder + "/", Tutorials),
        (Spacebox.Game.Player.Respawn.SpawnParameters.RelativePath, PlayerSpawn)
    };
}
