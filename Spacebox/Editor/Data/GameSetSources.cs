using Spacebox.Game.Resource.Content;

namespace Spacebox.Editor.Data;

public static class GameSetSources
{
    public static IEntrySource Blocks(GameSetProject project)
    {
        var flat = new ArrayEntrySource(project, GameSetFormat.BlocksFile, "", GameSetCatalog.Blocks);
        var folder = new EntryFolderSource(project, GameSetFormat.BlocksFolder, GameSetFormat.BlockEntryFile, GameSetCatalog.Blocks);
        return new CompositeEntrySource(GameSetCatalog.Blocks, () => project.Exists(GameSetFormat.BlocksFile) ? flat : folder, flat, folder);
    }

    public static IEntrySource Items(GameSetProject project)
    {
        var flat = new ArrayEntrySource(project, GameSetFormat.ItemsFile, "", GameSetCatalog.Items);
        var folder = new EntryFolderSource(project, GameSetFormat.ItemsFolder, GameSetFormat.ItemEntryFile, GameSetCatalog.Items);
        return new CompositeEntrySource(GameSetCatalog.Items, () => project.Exists(GameSetFormat.ItemsFile) ? flat : folder, flat, folder);
    }

    public static IEntrySource Blueprints(GameSetProject project)
    {
        var flat = new ArrayEntrySource(project, GameSetFormat.CraftingFile, "Blueprints", GameSetCatalog.Blueprints);
        var loose = new ArrayEntrySource(project, GameSetFormat.CraftingFolder + "/" + GameSetFormat.LooseBlueprintsFile, "Blueprints", GameSetCatalog.Blueprints);
        var entries = new EntryFolderSource(project, new[] { GameSetFormat.BlocksFolder, GameSetFormat.ItemsFolder }, GameSetFormat.BlueprintFile, GameSetCatalog.Blueprints, false);
        return new CompositeEntrySource(GameSetCatalog.Blueprints, () => project.Exists(GameSetFormat.CraftingFile) ? flat : loose, flat, loose, entries);
    }

    public static IEntrySource Categories(GameSetProject project)
    {
        var flat = new ArrayEntrySource(project, GameSetFormat.CraftingFile, "Categories", GameSetCatalog.CraftingCategories);
        var folder = new ArrayEntrySource(project, GameSetFormat.CraftingFolder + "/" + GameSetFormat.CategoriesFile, "Categories", GameSetCatalog.CraftingCategories);
        return new CompositeEntrySource(GameSetCatalog.CraftingCategories, () => project.Exists(GameSetFormat.CraftingFile) ? flat : folder, flat, folder);
    }

    public static IEntrySource ProcessingRecipes(GameSetProject project)
    {
        var flat = new GroupedArrayEntrySource(project, GameSetFormat.RecipesFile, "RecipeCatalog", "Type", "Recipes", GameSetCatalog.ProcessingRecipes);
        var folder = new RecipeGroupFolderSource(project, GameSetFormat.RecipesFolder, GameSetCatalog.ProcessingRecipes);
        return new CompositeEntrySource(GameSetCatalog.ProcessingRecipes, () => project.Exists(GameSetFormat.RecipesFile) ? flat : folder, flat, folder);
    }
}
