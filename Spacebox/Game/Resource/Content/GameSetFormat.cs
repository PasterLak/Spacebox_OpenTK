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

    public const string AdaptersFolder = "Bases";

    public static bool IsIgnoredPath(string root, string path)
    {
        string relative = Path.GetRelativePath(root, path);
        string folder = Path.GetDirectoryName(relative);
        if (string.IsNullOrEmpty(folder)) return false;

        var segments = folder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(segments[0], AdaptersFolder, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var segment in segments)
        {
            if (segment.StartsWith('_')) return true;
        }
        return false;
    }

    public static string AdapterFolder(string extensionFolder, InstalledGameSet baseSet)
    {
        if (string.IsNullOrEmpty(extensionFolder) || baseSet == null) return null;
        foreach (var name in new[] { baseSet.Id, baseSet.FolderName })
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            string path = Path.Combine(extensionFolder, AdaptersFolder, name);
            if (Directory.Exists(path)) return path;
        }
        return null;
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
