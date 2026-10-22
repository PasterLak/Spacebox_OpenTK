using System.Text.Json.Nodes;

namespace Spacebox.Game.Resource.Content.Conversion;

public sealed class RecipeFilesWriter
{
    private readonly string _root;
    private readonly ConversionResult _result;

    public RecipeFilesWriter(string root, ConversionResult result)
    {
        _root = root;
        _result = result;
    }

    public void ConvertCrafting(Func<string, string> entryDirectoryOf)
    {
        string path = Path.Combine(_root, GameSetFormat.CraftingFile);
        if (!File.Exists(path)) return;

        if (ContentJson.ParseFile(path, _result.Errors) is not JsonObject crafting)
        {
            _result.Errors.Add($"'{path}' is not a JSON object");
            return;
        }

        foreach (var property in crafting)
        {
            if (!IsKnown(property.Key, "Categories", "Blueprints")) _result.Errors.Add($"crafting.json has an unknown section '{property.Key}'");
        }
        if (_result.Errors.Count > 0) return;

        string folder = Path.Combine(_root, GameSetFormat.CraftingFolder);
        if (ContentJson.Get(crafting, "Categories") is JsonArray categories && categories.Count > 0)
            PrependArray(Path.Combine(folder, GameSetFormat.CategoriesFile), "Categories", categories);

        var loose = new JsonArray();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ContentJson.Get(crafting, "Blueprints") is JsonArray blueprints)
        {
            foreach (var node in blueprints)
            {
                if (node is not JsonObject blueprint) continue;
                string product = LayerContentReader.BlueprintKey(blueprint);
                if (product.Length > 0 && !seen.Add(product))
                {
                    _result.Log.Add($"Blueprint for '{product}' is defined twice; the ignored copy was dropped");
                    continue;
                }

                string directory = product.Length > 0 ? entryDirectoryOf(product) : null;
                if (directory != null)
                    ContentJson.WriteFile(Path.Combine(directory, GameSetFormat.BlueprintFile), blueprint);
                else
                    loose.Add(blueprint.DeepClone());
                _result.Blueprints++;
            }
        }

        if (loose.Count > 0) PrependArray(Path.Combine(folder, GameSetFormat.LooseBlueprintsFile), "Blueprints", loose);

        File.Delete(path);
        _result.Log.Add($"crafting.json: {_result.Blueprints} blueprints moved ({loose.Count} without an entry folder to Crafting/{GameSetFormat.LooseBlueprintsFile})");
    }

    public void ConvertRecipes()
    {
        string path = Path.Combine(_root, GameSetFormat.RecipesFile);
        if (!File.Exists(path)) return;

        if (ContentJson.ParseFile(path, _result.Errors) is not JsonObject root || ContentJson.Get(root, "RecipeCatalog") is not JsonArray catalog)
        {
            _result.Errors.Add($"'{path}' has no RecipeCatalog");
            return;
        }

        var groups = new List<(string Type, JsonArray Recipes)>();
        foreach (var node in catalog)
        {
            if (node is not JsonObject group) continue;
            string type = ContentJson.GetString(group, "Type") ?? "unknown";
            int index = groups.FindIndex(g => string.Equals(g.Type, type, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                groups.Add((type, new JsonArray()));
                index = groups.Count - 1;
            }

            if (ContentJson.Get(group, "Recipes") is JsonArray recipes)
            {
                foreach (var recipe in recipes)
                {
                    groups[index].Recipes.Add(recipe?.DeepClone());
                    _result.Recipes++;
                }
            }
        }

        string folder = Path.Combine(_root, GameSetFormat.RecipesFolder);
        Directory.CreateDirectory(folder);
        foreach (var group in groups)
        {
            string file = Path.Combine(folder, SafeFileName(group.Type) + ".json");
            var existing = File.Exists(file) ? ContentJson.ParseFile(file, _result.Errors) as JsonObject : null;
            if (File.Exists(file) && (existing == null || !string.Equals(ContentJson.GetString(existing, "Type"), group.Type, StringComparison.OrdinalIgnoreCase)))
            {
                _result.Errors.Add($"'{file}' exists and is not a '{group.Type}' recipe group");
                return;
            }

            var merged = new JsonArray();
            foreach (var recipe in group.Recipes) merged.Add(recipe?.DeepClone());
            if (existing != null && ContentJson.Get(existing, "Recipes") is JsonArray old)
            {
                foreach (var recipe in old) merged.Add(recipe?.DeepClone());
            }

            ContentJson.WriteFile(file, new JsonObject { ["Type"] = group.Type, ["Recipes"] = merged });
        }

        File.Delete(path);
        _result.Log.Add($"recipes.json: {_result.Recipes} recipes moved to {GameSetFormat.RecipesFolder}/");
    }

    private void PrependArray(string file, string key, JsonArray items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        var root = File.Exists(file) ? ContentJson.ParseFile(file, _result.Errors) as JsonObject : null;
        root ??= new JsonObject();

        var merged = new JsonArray();
        foreach (var item in items) merged.Add(item?.DeepClone());
        if (ContentJson.Get(root, key) is JsonArray old)
        {
            foreach (var item in old) merged.Add(item?.DeepClone());
        }

        ContentJson.Set(root, key, merged);
        ContentJson.WriteFile(file, root);
    }

    private static bool IsKnown(string key, params string[] known)
    {
        foreach (var name in known)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static string SafeFileName(string name)
    {
        var chars = name.Trim().ToLowerInvariant().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-') chars[i] = '_';
        }
        return chars.Length == 0 ? "unknown" : new string(chars);
    }
}
