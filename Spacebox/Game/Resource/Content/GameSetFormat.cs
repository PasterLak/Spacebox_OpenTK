namespace Spacebox.Game.Resource.Content;

public static class GameSetFormat
{
    public const string ConfigFile = "config.json";
    public const string BlocksFile = "blocks.json";
    public const string ItemsFile = "items.json";
    public const string CraftingFile = "crafting.json";
    public const string RecipesFile = "recipes.json";

    public const string BlocksFolder = "Blocks";
    public const string ItemsFolder = "Items";
    public const string EmissionsFolder = "Emissions";
    public const string SoundsFolder = "Sounds";
    public const string RecipesFolder = "Recipes";
    public const string CraftingFolder = "Crafting";

    public const string BlockEntryFile = "block.json";
    public const string ItemEntryFile = "item.json";
    public const string BlueprintFile = "blueprint.json";
    public const string EntryRecipesFile = "recipes.json";
    public const string OrderFile = "order.json";
    public const string CategoriesFile = "categories.json";
    public const string LooseBlueprintsFile = "blueprints.json";

    public static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };
    public static readonly string[] SoundExtensions = { ".wav", ".ogg" };

    public static bool HasExtension(string path, string[] extensions)
    {
        string extension = Path.GetExtension(path);
        foreach (var candidate in extensions)
        {
            if (string.Equals(extension, candidate, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static bool IsIgnoredPath(string root, string path)
    {
        string relative = Path.GetRelativePath(root, path);
        string folder = Path.GetDirectoryName(relative);
        if (string.IsNullOrEmpty(folder)) return false;

        foreach (var segment in folder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.StartsWith('_')) return true;
        }
        return false;
    }

    public static bool IsInEmissionsFolder(string root, string path)
    {
        string folder = Path.GetDirectoryName(Path.GetRelativePath(root, path));
        if (string.IsNullOrEmpty(folder)) return false;

        foreach (var segment in folder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (string.Equals(segment, EmissionsFolder, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
