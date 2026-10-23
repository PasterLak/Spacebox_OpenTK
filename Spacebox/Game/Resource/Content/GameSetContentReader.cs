namespace Spacebox.Game.Resource.Content;

public static class GameSetContentReader
{
    public static GameSetContent ReadFolder(string root, string id = "gameset")
    {
        return Read(new[] { new ContentLayer(id, root) });
    }

    public static GameSetContent Read(IReadOnlyList<ContentLayer> layers, ContentLayer fallback = null, IdScopes scopes = null)
    {
        var warnings = new List<string>();
        Func<ContentLayer, bool> isBase = layer => (layers.Count > 0 && ReferenceEquals(layer, layers[0])) || ReferenceEquals(layer, fallback);
        var blocks = new KeyedContentList("Block", warnings, isBase);
        var items = new KeyedContentList("Item", warnings, isBase);
        var categories = new KeyedContentList("Crafting category", warnings, isBase);
        var blueprints = new KeyedContentList("Blueprint", warnings, isBase);
        var recipes = new KeyedContentList("Recipe", warnings, isBase);

        var contents = new List<LayerContent>(layers.Count);
        for (int i = 0; i < layers.Count; i++)
        {
            var content = LayerContentReader.Read(layers[i], warnings);
            if (i == 0 && fallback != null && !SameRoot(layers[0], fallback)) content = WithFallback(content, fallback, warnings);
            contents.Add(content);
        }

        Func<string, bool> exists = key => blocks.Contains(key) || items.Contains(key);
        for (int i = 0; i < layers.Count; i++)
        {
            string ns = NamespaceOf(layers[i], scopes);
            AddAll(blocks, contents[i].Blocks, entry => Define(entry, ns, scopes, exists));
            AddAll(items, contents[i].Items, entry => Define(entry, ns, scopes, exists));
        }

        for (int i = 0; i < layers.Count; i++)
        {
            string ns = NamespaceOf(layers[i], scopes);
            AddAll(categories, contents[i].Categories, entry => entry.WithNamespace(ns));
            AddAll(blueprints, contents[i].Blueprints, entry => Reference(entry, entry.Key, ns, scopes, exists));
            AddAll(recipes, contents[i].Recipes, entry => RecipeReference(entry, ns, scopes, exists));
        }

        return new GameSetContent(layers, blocks.Entries, items.Entries, categories.Entries, blueprints.Entries, recipes.Entries, warnings);
    }

    private static LayerContent WithFallback(LayerContent own, ContentLayer fallback, List<string> warnings)
    {
        if (own.HasBlocks && own.HasItems && own.HasCrafting && own.HasRecipes) return own;

        var defaults = LayerContentReader.Read(fallback, warnings);
        var merged = new LayerContent();
        merged.Blocks.AddRange(own.HasBlocks ? own.Blocks : defaults.Blocks);
        merged.Items.AddRange(own.HasItems ? own.Items : defaults.Items);
        merged.Categories.AddRange(own.HasCrafting ? own.Categories : defaults.Categories);
        merged.Blueprints.AddRange(own.HasCrafting ? own.Blueprints : defaults.Blueprints);
        merged.Recipes.AddRange(own.HasRecipes ? own.Recipes : defaults.Recipes);
        return merged;
    }

    private static void AddAll(KeyedContentList target, List<ContentEntry> entries, Func<ContentEntry, ContentEntry> map)
    {
        foreach (var entry in entries) target.Add(map(entry));
    }

    private static string NamespaceOf(ContentLayer layer, IdScopes scopes)
    {
        return scopes != null && scopes.IsExtension(layer.Id) ? layer.Id.ToLowerInvariant() : null;
    }

    private static ContentEntry Define(ContentEntry entry, string ns, IdScopes scopes, Func<string, bool> exists)
    {
        if (scopes == null || string.IsNullOrEmpty(entry.Key)) return entry.WithNamespace(ns);
        string key = scopes.Define(entry.Key, ns, exists);
        int colon = key.IndexOf(':');
        return entry.WithKey(key, colon < 0 ? null : key.Substring(0, colon));
    }

    private static ContentEntry Reference(ContentEntry entry, string raw, string ns, IdScopes scopes, Func<string, bool> exists)
    {
        if (scopes == null || string.IsNullOrEmpty(raw)) return entry.WithNamespace(ns);
        return Qualified(entry, ns, scopes, exists).WithKey(scopes.Resolve(raw, ns, exists), ns);
    }

    private static ContentEntry RecipeReference(ContentEntry entry, string ns, IdScopes scopes, Func<string, bool> exists)
    {
        int bar = entry.Key.IndexOf('|');
        if (scopes == null || bar < 0) return entry.WithNamespace(ns);
        return Qualified(entry, ns, scopes, exists).WithKey(entry.Key.Substring(0, bar + 1) + scopes.Resolve(entry.Key.Substring(bar + 1), ns, exists), ns);
    }

    private static readonly string[] ReferenceObjects = { "Product", "Ingredient" };

    private static ContentEntry Qualified(ContentEntry entry, string ns, IdScopes scopes, Func<string, bool> exists)
    {
        if (ns == null || entry.Data == null) return entry;
        var data = (System.Text.Json.Nodes.JsonObject)entry.Data.DeepClone();
        foreach (var name in ReferenceObjects)
            if (ContentJson.Get(data, name) is System.Text.Json.Nodes.JsonObject reference) QualifyItem(reference, ns, scopes, exists);
        if (ContentJson.Get(data, "Ingredients") is System.Text.Json.Nodes.JsonArray ingredients)
            foreach (var ingredient in ingredients)
                if (ingredient is System.Text.Json.Nodes.JsonObject obj) QualifyItem(obj, ns, scopes, exists);
        return new ContentEntry(entry.Key, data, entry.Layer, entry.Source, entry.SearchDirectories, entry.Namespace);
    }

    private static void QualifyItem(System.Text.Json.Nodes.JsonObject reference, string ns, IdScopes scopes, Func<string, bool> exists)
    {
        string item = ContentJson.GetString(reference, "Item");
        if (string.IsNullOrEmpty(item) || item.Contains(':')) return;
        string resolved = scopes.Resolve(item.ToLowerInvariant(), ns, exists);
        if (resolved.Contains(':')) ContentJson.Set(reference, "Item", resolved);
    }

    private static bool SameRoot(ContentLayer a, ContentLayer b)
    {
        return string.Equals(Path.GetFullPath(a.Root).TrimEnd('\\', '/'), Path.GetFullPath(b.Root).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }
}
