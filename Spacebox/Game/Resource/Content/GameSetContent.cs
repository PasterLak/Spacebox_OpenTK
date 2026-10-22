using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content;

public sealed class GameSetContent
{
    public IReadOnlyList<ContentLayer> Layers { get; }
    public IReadOnlyList<ContentEntry> Blocks { get; }
    public IReadOnlyList<ContentEntry> Items { get; }
    public IReadOnlyList<ContentEntry> Categories { get; }
    public IReadOnlyList<ContentEntry> Blueprints { get; }
    public IReadOnlyList<ContentEntry> Recipes { get; }
    public IReadOnlyList<string> Warnings { get; }

    public GameSetContent(IReadOnlyList<ContentLayer> layers, IReadOnlyList<ContentEntry> blocks, IReadOnlyList<ContentEntry> items,
        IReadOnlyList<ContentEntry> categories, IReadOnlyList<ContentEntry> blueprints, IReadOnlyList<ContentEntry> recipes, IReadOnlyList<string> warnings)
    {
        Layers = layers;
        Blocks = blocks;
        Items = items;
        Categories = categories;
        Blueprints = blueprints;
        Recipes = recipes;
        Warnings = warnings;
    }

    public JsonArray BlocksJson() => ToArray(Blocks);

    public JsonArray ItemsJson() => ToArray(Items);

    public JsonObject CraftingJson()
    {
        return new JsonObject
        {
            ["Categories"] = ToArray(Categories),
            ["Blueprints"] = ToArray(Blueprints)
        };
    }

    public JsonObject RecipesJson()
    {
        var groups = new List<(string Type, JsonArray Recipes)>();
        foreach (var entry in Recipes)
        {
            string type = ContentJson.GetString(entry.Data, "Type") ?? "unknown";
            int index = groups.FindIndex(g => string.Equals(g.Type, type, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                groups.Add((type, new JsonArray()));
                index = groups.Count - 1;
            }
            groups[index].Recipes.Add(entry.Data.DeepClone());
        }

        var catalog = new JsonArray();
        foreach (var group in groups)
            catalog.Add(new JsonObject { ["Type"] = group.Type, ["Recipes"] = group.Recipes });

        return new JsonObject { ["RecipeCatalog"] = catalog };
    }

    public JsonNode FlatFile(string fileName)
    {
        if (string.Equals(fileName, GameSetFormat.BlocksFile, StringComparison.OrdinalIgnoreCase)) return BlocksJson();
        if (string.Equals(fileName, GameSetFormat.ItemsFile, StringComparison.OrdinalIgnoreCase)) return ItemsJson();
        if (string.Equals(fileName, GameSetFormat.CraftingFile, StringComparison.OrdinalIgnoreCase)) return CraftingJson();
        if (string.Equals(fileName, GameSetFormat.RecipesFile, StringComparison.OrdinalIgnoreCase)) return RecipesJson();
        throw new ArgumentException($"'{fileName}' is not a content file", nameof(fileName));
    }

    public ContentEntry FindBlock(string id) => Find(Blocks, id);

    public ContentEntry FindItem(string id) => Find(Items, id);

    private static ContentEntry Find(IReadOnlyList<ContentEntry> entries, string id)
    {
        foreach (var entry in entries)
        {
            if (string.Equals(entry.Key, id, StringComparison.OrdinalIgnoreCase)) return entry;
        }
        return null;
    }

    private static JsonArray ToArray(IReadOnlyList<ContentEntry> entries)
    {
        var array = new JsonArray();
        foreach (var entry in entries) array.Add(entry.Data.DeepClone());
        return array;
    }
}
