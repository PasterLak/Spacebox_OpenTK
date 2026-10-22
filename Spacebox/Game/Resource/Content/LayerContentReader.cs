using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content;

public sealed class LayerContentReader
{
    private readonly ContentLayer _layer;
    private readonly ICollection<string> _warnings;
    private readonly LayerContent _content = new LayerContent();

    private LayerContentReader(ContentLayer layer, ICollection<string> warnings)
    {
        _layer = layer;
        _warnings = warnings;
    }

    public static LayerContent Read(ContentLayer layer, ICollection<string> warnings)
    {
        var reader = new LayerContentReader(layer, warnings);
        reader.ReadAll();
        return reader._content;
    }

    private void ReadAll()
    {
        if (!Directory.Exists(_layer.Root))
        {
            _warnings.Add($"GameSet folder '{_layer.Root}' was not found");
            return;
        }

        ReadFlatArray(GameSetFormat.BlocksFile, _content.Blocks, () => _content.HasBlocks = true);
        ReadFlatArray(GameSetFormat.ItemsFile, _content.Items, () => _content.HasItems = true);
        ReadFlatCrafting(_layer.PathOf(GameSetFormat.CraftingFile));
        ReadCraftingFolder();
        ReadFlatRecipes(_layer.PathOf(GameSetFormat.RecipesFile));
        ReadRecipesFolder();

        var blockEntries = ReadEntryFolder(GameSetFormat.BlocksFolder, GameSetFormat.BlockEntryFile, _content.Blocks);
        var itemEntries = ReadEntryFolder(GameSetFormat.ItemsFolder, GameSetFormat.ItemEntryFile, _content.Items);
        if (blockEntries.Count > 0) _content.HasBlocks = true;
        if (itemEntries.Count > 0) _content.HasItems = true;

        foreach (var entry in blockEntries) ReadEntryExtras(entry);
        foreach (var entry in itemEntries) ReadEntryExtras(entry);
    }

    private void ReadFlatArray(string fileName, List<ContentEntry> target, Action found)
    {
        string path = _layer.PathOf(fileName);
        if (!File.Exists(path)) return;

        found();
        var node = ContentJson.ParseFile(path, _warnings);
        if (node is not JsonArray array)
        {
            if (node != null) _warnings.Add($"'{path}' must be an array");
            return;
        }

        foreach (var element in array)
        {
            if (element is JsonObject obj) target.Add(Entry(KeyOf(obj, "ID"), obj, fileName, null));
        }
    }

    private void ReadFlatCrafting(string path)
    {
        if (!File.Exists(path)) return;

        _content.HasCrafting = true;
        if (ContentJson.ParseFile(path, _warnings) is not JsonObject root) return;
        AddCrafting(root, Path.GetRelativePath(_layer.Root, path));
    }

    private void ReadCraftingFolder()
    {
        string folder = _layer.PathOf(GameSetFormat.CraftingFolder);
        foreach (var path in SortedJsonFiles(folder)) ReadFlatCrafting(path);
    }

    private void AddCrafting(JsonObject root, string source)
    {
        if (ContentJson.Get(root, "Categories") is JsonArray categories)
        {
            foreach (var element in categories)
            {
                if (element is JsonObject category) _content.Categories.Add(Entry(KeyOf(category, "Id"), category, source, null));
            }
        }

        if (ContentJson.Get(root, "Blueprints") is JsonArray blueprints)
        {
            foreach (var element in blueprints)
            {
                if (element is JsonObject blueprint) _content.Blueprints.Add(Entry(BlueprintKey(blueprint), blueprint, source, null));
            }
        }
    }

    private void ReadFlatRecipes(string path)
    {
        if (!File.Exists(path)) return;

        _content.HasRecipes = true;
        var node = ContentJson.ParseFile(path, _warnings);
        string source = Path.GetRelativePath(_layer.Root, path);

        if (node is JsonObject root && ContentJson.Get(root, "RecipeCatalog") is JsonArray catalog)
        {
            foreach (var group in catalog)
            {
                if (group is JsonObject groupObject) AddRecipeGroup(groupObject, source);
            }
            return;
        }

        if (node is JsonObject single)
        {
            AddRecipeGroup(single, source);
            return;
        }

        if (node != null) _warnings.Add($"'{path}' must contain RecipeCatalog or Type and Recipes");
    }

    private void ReadRecipesFolder()
    {
        foreach (var path in SortedJsonFiles(_layer.PathOf(GameSetFormat.RecipesFolder))) ReadFlatRecipes(path);
    }

    private void AddRecipeGroup(JsonObject group, string source)
    {
        string type = ContentJson.GetString(group, "Type") ?? "unknown";
        if (ContentJson.Get(group, "Recipes") is not JsonArray recipes) return;

        foreach (var element in recipes)
        {
            if (element is not JsonObject recipe) continue;
            var copy = (JsonObject)recipe.DeepClone();
            ContentJson.Set(copy, "Type", JsonValue.Create(type));
            _content.Recipes.Add(Entry(RecipeKey(copy), copy, source, null));
        }
    }

    private List<ContentEntry> ReadEntryFolder(string folderName, string entryFileName, List<ContentEntry> target)
    {
        string root = _layer.PathOf(folderName);
        var entries = new List<ContentEntry>();

        foreach (var path in EntryFolderScanner.FindEntryFiles(root, entryFileName))
        {
            var node = ContentJson.ParseFile(path, _warnings);
            if (node is not JsonObject obj)
            {
                if (node != null) _warnings.Add($"'{path}' must contain one JSON object");
                continue;
            }

            entries.Add(Entry(KeyOf(obj, "ID"), obj, Path.GetRelativePath(_layer.Root, path), Path.GetDirectoryName(path)));
        }

        entries = EntryFolderScanner.ApplyOrder(entries, e => e.Key, EntryFolderScanner.ReadOrder(root, _warnings));
        target.AddRange(entries);
        return entries;
    }

    private void ReadEntryExtras(ContentEntry entry)
    {
        string directory = entry.SearchDirectories.Count > 0 ? entry.SearchDirectories[0] : null;
        if (directory == null || string.IsNullOrEmpty(entry.Key)) return;

        ReadEntryBlueprint(entry, Path.Combine(directory, GameSetFormat.BlueprintFile), directory);
        ReadEntryRecipes(entry, Path.Combine(directory, GameSetFormat.EntryRecipesFile), directory);
    }

    private void ReadEntryBlueprint(ContentEntry owner, string path, string directory)
    {
        if (!File.Exists(path)) return;

        _content.HasCrafting = true;
        if (ContentJson.ParseFile(path, _warnings) is not JsonObject blueprint) return;

        var copy = (JsonObject)blueprint.DeepClone();
        if (ContentJson.Get(copy, "Product") is not JsonObject product)
        {
            product = new JsonObject { ["Item"] = owner.Key, ["Quantity"] = 1 };
            ContentJson.Set(copy, "Product", product);
        }
        else if (string.IsNullOrWhiteSpace(ContentJson.GetString(product, "Item")))
        {
            ContentJson.Set(product, "Item", JsonValue.Create(owner.Key));
        }

        _content.Blueprints.Add(Entry(BlueprintKey(copy), copy, Path.GetRelativePath(_layer.Root, path), directory));
    }

    private void ReadEntryRecipes(ContentEntry owner, string path, string directory)
    {
        if (!File.Exists(path)) return;

        _content.HasRecipes = true;
        var node = ContentJson.ParseFile(path, _warnings);
        var recipes = node as JsonArray ?? (node is JsonObject obj ? ContentJson.Get(obj, "Recipes") as JsonArray : null);
        if (recipes == null)
        {
            if (node != null) _warnings.Add($"'{path}' must be an array of recipes");
            return;
        }

        string source = Path.GetRelativePath(_layer.Root, path);
        foreach (var element in recipes)
        {
            if (element is not JsonObject recipe) continue;

            var copy = (JsonObject)recipe.DeepClone();
            if (string.IsNullOrWhiteSpace(ContentJson.GetString(copy, "Type")))
            {
                _warnings.Add($"A recipe in '{path}' has no Type and was skipped");
                continue;
            }

            if (ContentJson.Get(copy, "Ingredient") is not JsonObject ingredient)
                ContentJson.Set(copy, "Ingredient", new JsonObject { ["Item"] = owner.Key, ["Quantity"] = 1 });
            else if (string.IsNullOrWhiteSpace(ContentJson.GetString(ingredient, "Item")))
                ContentJson.Set(ingredient, "Item", JsonValue.Create(owner.Key));

            _content.Recipes.Add(Entry(RecipeKey(copy), copy, source, directory));
        }
    }

    private ContentEntry Entry(string key, JsonObject data, string source, string directory)
    {
        return ContentEntry.Create(key, data, _layer, source.Replace('\\', '/'), directory);
    }

    public static string KeyOf(JsonObject obj, string idKey)
    {
        return ContentJson.GetString(obj, idKey)?.Trim().ToLowerInvariant() ?? "";
    }

    public static string BlueprintKey(JsonObject blueprint)
    {
        return ContentJson.Get(blueprint, "Product") is JsonObject product ? KeyOf(product, "Item") : "";
    }

    public static string RecipeKey(JsonObject recipe)
    {
        string type = KeyOf(recipe, "Type");
        string ingredient = ContentJson.Get(recipe, "Ingredient") is JsonObject obj ? KeyOf(obj, "Item") : "";
        return string.IsNullOrEmpty(ingredient) ? "" : type + "|" + ingredient;
    }

    private static IEnumerable<string> SortedJsonFiles(string folder)
    {
        if (!Directory.Exists(folder)) return Array.Empty<string>();

        var files = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        return files;
    }
}
