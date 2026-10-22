using System.Text.Json.Nodes;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Mobs;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;

namespace Spacebox.Editor.Data;

public static class GameSetCatalog
{
    public const string BlocksFile = "blocks.json";
    public const string ItemsFile = "items.json";
    public const string CraftingFile = "crafting.json";
    public const string RecipesFile = "recipes.json";
    public const string ConfigFile = "config.json";
    public const string ProjectilesFolder = "Projectiles";
    public const string LootFolder = "Generation/Loot";
    public const string MobsFolder = "Mobs";
    public const string AsteroidsFolder = "Generation/Asteroids";
    public const string BiomesFolder = "Generation/Biomes";
    public const string GeneratorFile = "Generation/generator.json";

    private static readonly Dictionary<string, Type> ItemTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        { "item", typeof(ItemJSON) },
        { "weapon", typeof(WeaponItemJSON) },
        { "drill", typeof(DrillItemJSON) },
        { "consumable", typeof(ConsumableItemJSON) },
        { "mob_spawner", typeof(MobSpawnerItemJSON) },
        { "equipment", typeof(EquipmentJSON) }
    };

    private static readonly Dictionary<string, Type> FileSchemas = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        { ConfigFile, typeof(GameSetLoader.ModConfig) },
        { "ships.json", typeof(ShipPhysicsSettings) },
        { "disassembly.json", typeof(DisassemblyJSON) },
        { GeneratorFile, typeof(GeneratorJSON) },
        { "Optional/lighting.json", typeof(GameSetLoader.Modlighting) },
        { CraftingFile, typeof(CraftingJSON) },
        { RecipesFile, typeof(RecipesJSON) }
    };

    public static IEnumerable<string> ItemTypeNames => ItemTypes.Keys;

    public static IEnumerable<Type> SchemaRoots()
    {
        foreach (var type in ItemTypes.Values) yield return type;
        foreach (var type in FileSchemas.Values) yield return type;
        yield return typeof(BlockJSON);
        yield return typeof(BlueprintJSON);
        yield return typeof(RecipeJSON);
        yield return typeof(RecipejSONType);
        yield return typeof(CraftingJSON);
        yield return typeof(CraftingCategory);
        yield return typeof(ProjectileJSON);
        yield return typeof(LootEntry);
        yield return typeof(MobJSON);
        yield return typeof(AsteroidJSON);
        yield return typeof(BiomeJSON);
    }
    public static IEnumerable<string> BlockTypeNames => BlockFactory.Types;

    public static void RegisterItemType(string type, Type schema)
    {
        ItemTypes[type] = schema;
    }

    public static Type ItemSchema(JsonNode node)
    {
        var type = node is JsonObject obj ? EditorJson.GetString(obj, "Type", "item") : "item";
        return ItemTypes.TryGetValue(type, out var schema) ? schema : typeof(ItemJSON);
    }

    private static bool IsEntryFile(string relativePath, string folder, string entryFile)
    {
        return relativePath.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)
            && Game.Resource.Content.EntryFiles.IsEntryFile(relativePath, entryFile, _ => false);
    }

    public static Type FileSchema(string relativePath)
    {
        relativePath = GameSetProject.Normalize(relativePath);
        if (FileSchemas.TryGetValue(relativePath, out var schema)) return schema;
        if (relativePath.StartsWith(BlocksFile, StringComparison.OrdinalIgnoreCase)) return typeof(List<BlockJSON>);
        string fileName = Path.GetFileName(relativePath);
        if (IsEntryFile(relativePath, Game.Resource.Content.GameSetFormat.BlocksFolder, Game.Resource.Content.GameSetFormat.BlockEntryFile)) return typeof(BlockJSON);
        if (IsEntryFile(relativePath, Game.Resource.Content.GameSetFormat.ItemsFolder, Game.Resource.Content.GameSetFormat.ItemEntryFile)) return typeof(ItemJSON);
        if (fileName.Equals(Game.Resource.Content.GameSetFormat.BlueprintFile, StringComparison.OrdinalIgnoreCase)) return typeof(BlueprintJSON);
        if (fileName.Equals(Game.Resource.Content.GameSetFormat.EntryRecipesFile, StringComparison.OrdinalIgnoreCase)) return typeof(List<RecipeJSON>);
        if (relativePath.StartsWith(Game.Resource.Content.GameSetFormat.RecipesFolder + "/", StringComparison.OrdinalIgnoreCase)) return typeof(RecipejSONType);
        if (relativePath.StartsWith(Game.Resource.Content.GameSetFormat.CraftingFolder + "/", StringComparison.OrdinalIgnoreCase)) return typeof(CraftingJSON);
        if (relativePath.StartsWith(ProjectilesFolder + "/", StringComparison.OrdinalIgnoreCase)) return typeof(ProjectileJSON);
        if (relativePath.StartsWith(LootFolder + "/", StringComparison.OrdinalIgnoreCase)) return typeof(List<LootEntry>);
        if (relativePath.StartsWith(MobsFolder + "/", StringComparison.OrdinalIgnoreCase)) return typeof(MobJSON);
        if (relativePath.StartsWith(AsteroidsFolder + "/", StringComparison.OrdinalIgnoreCase)) return typeof(AsteroidJSON);
        if (relativePath.StartsWith(BiomesFolder + "/", StringComparison.OrdinalIgnoreCase)) return typeof(BiomeJSON);
        return null;
    }

    public static readonly EntryDefinition Blocks = new EntryDefinition
    {
        IdKey = "ID",
        Schema = _ => typeof(BlockJSON),
        Label = node => NameOf(node),
        NewIdPrefix = "new_block",
        Template = () => new JsonObject
        {
            ["ID"] = "new_block",
            ["Name"] = "New Block",
            ["Type"] = "block",
            ["Category"] = "building",
            ["PowerToDrill"] = 1,
            ["Mass"] = 1,
            ["Durability"] = 10,
            ["Textures"] = new JsonObject { ["Active"] = new JsonObject { ["All"] = "" } }
        }
    };

    public static readonly EntryDefinition Items = new EntryDefinition
    {
        IdKey = "ID",
        Schema = ItemSchema,
        Label = node => NameOf(node),
        NewIdPrefix = "new_item",
        Template = () => new JsonObject
        {
            ["ID"] = "new_item",
            ["Name"] = "New Item",
            ["Type"] = "item",
            ["Category"] = "items",
            ["Sprite"] = "",
            ["MaxStack"] = 64,
            ["ModelDepth"] = 1
        }
    };

    public static readonly EntryDefinition Projectiles = new EntryDefinition
    {
        IdKey = "",
        Schema = _ => typeof(ProjectileJSON),
        NewIdPrefix = "p_new",
        Template = () => new JsonObject
        {
            ["Speed"] = 50,
            ["Mass"] = 10,
            ["MaxTravelDistance"] = 150,
            ["Length"] = 1,
            ["Thickness"] = 0.08,
            ["Color"] = new JsonObject { ["r"] = 255, ["g"] = 200, ["b"] = 0 },
            ["Damage"] = 10,
            ["DamageBlocks"] = 5,
            ["Penetration"] = 50,
            ["RicochetAngle"] = 15,
            ["PossibleRicochets"] = 1,
            ["MaximumBlocksCanDestroy"] = 1,
            ["DropBlock"] = false
        }
    };

    public static readonly EntryDefinition Loot = new EntryDefinition
    {
        IdKey = "",
        Schema = _ => typeof(List<LootEntry>),
        NewIdPrefix = "new_loot",
        Template = () => new JsonArray { new JsonObject { ["ID"] = "", ["Probability"] = 100, ["MinCount"] = 1, ["MaxCount"] = 1 } }
    };

    public static readonly EntryDefinition Mobs = new EntryDefinition
    {
        IdKey = "Id",
        Schema = _ => typeof(MobJSON),
        Label = node => NameOf(node),
        NewIdPrefix = "new_mob",
        Template = () => JsonSchema.CreateEmpty(typeof(MobJSON))
    };

    public static readonly EntryDefinition Asteroids = new EntryDefinition
    {
        IdKey = "Id",
        Schema = _ => typeof(AsteroidJSON),
        Label = node => NameOf(node),
        NewIdPrefix = "new_asteroid",
        Template = () => JsonSchema.CreateEmpty(typeof(AsteroidJSON))
    };

    public static readonly EntryDefinition Biomes = new EntryDefinition
    {
        IdKey = "Id",
        Schema = _ => typeof(BiomeJSON),
        Label = node => NameOf(node),
        NewIdPrefix = "new_biome",
        Template = () => JsonSchema.CreateEmpty(typeof(BiomeJSON))
    };

    public static readonly EntryDefinition Blueprints = new EntryDefinition
    {
        IdKey = "",
        Schema = _ => typeof(BlueprintJSON),
        Label = node => node is JsonObject obj && EditorJson.Get(obj, "Product") is JsonObject product ? EditorJson.GetString(product, "Item") : "",
        Template = () => new JsonObject
        {
            ["Product"] = new JsonObject { ["Item"] = "", ["Quantity"] = 1 },
            ["Ingredients"] = new JsonArray { new JsonObject { ["Item"] = "", ["Quantity"] = 1 } }
        }
    };

    public static readonly EntryDefinition CraftingCategories = new EntryDefinition
    {
        IdKey = "Id",
        Schema = _ => typeof(CraftingCategory),
        Label = node => NameOf(node),
        NewIdPrefix = "new_category",
        Template = () => new JsonObject { ["Id"] = "new_category", ["Name"] = "New Category", ["Icon"] = "" }
    };

    public static readonly EntryDefinition ProcessingRecipes = new EntryDefinition
    {
        IdKey = "",
        Schema = _ => typeof(RecipeJSON),
        Label = node => RecipeLabel(node),
        Template = () => new JsonObject
        {
            ["DurationSec"] = 5.0,
            ["PowerPerTickRequared"] = 5,
            ["Ingredient"] = new JsonObject { ["Item"] = "", ["Quantity"] = 1 },
            ["Product"] = new JsonObject { ["Item"] = "", ["Quantity"] = 1 }
        }
    };

    public static string NameOf(JsonNode node)
    {
        if (node is not JsonObject obj) return "";
        var name = EditorJson.GetString(obj, "Name");
        var id = EditorJson.GetString(obj, "ID", EditorJson.GetString(obj, "Id"));
        if (string.IsNullOrWhiteSpace(name)) return id;
        return string.IsNullOrWhiteSpace(id) ? name : $"{name} ({id})";
    }

    public static string RecipeLabel(JsonNode node)
    {
        if (node is not JsonObject obj) return "";
        var input = EditorJson.Get(obj, "Ingredient") is JsonObject ingredient ? EditorJson.GetString(ingredient, "Item") : "?";
        var output = EditorJson.Get(obj, "Product") is JsonObject product ? EditorJson.GetString(product, "Item") : "?";
        return $"{input} -> {output}";
    }
}
