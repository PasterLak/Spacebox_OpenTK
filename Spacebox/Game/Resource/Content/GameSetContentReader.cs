namespace Spacebox.Game.Resource.Content;

public static class GameSetContentReader
{
    public static GameSetContent ReadFolder(string root, string id = "gameset")
    {
        return Read(new[] { new ContentLayer(id, root) });
    }

    public static GameSetContent Read(IReadOnlyList<ContentLayer> layers, ContentLayer fallback = null)
    {
        var warnings = new List<string>();
        var blocks = new KeyedContentList("Block", warnings);
        var items = new KeyedContentList("Item", warnings);
        var categories = new KeyedContentList("Crafting category", warnings);
        var blueprints = new KeyedContentList("Blueprint", warnings);
        var recipes = new KeyedContentList("Recipe", warnings);

        for (int i = 0; i < layers.Count; i++)
        {
            var content = LayerContentReader.Read(layers[i], warnings);
            if (i == 0 && fallback != null && !SameRoot(layers[0], fallback)) content = WithFallback(content, fallback, warnings);

            AddAll(blocks, content.Blocks);
            AddAll(items, content.Items);
            AddAll(categories, content.Categories);
            AddAll(blueprints, content.Blueprints);
            AddAll(recipes, content.Recipes);
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

    private static void AddAll(KeyedContentList target, List<ContentEntry> entries)
    {
        foreach (var entry in entries) target.Add(entry);
    }

    private static bool SameRoot(ContentLayer a, ContentLayer b)
    {
        return string.Equals(Path.GetFullPath(a.Root).TrimEnd('\\', '/'), Path.GetFullPath(b.Root).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }
}
