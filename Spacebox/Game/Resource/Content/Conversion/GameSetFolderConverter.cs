namespace Spacebox.Game.Resource.Content.Conversion;

public static class GameSetFolderConverter
{
    private static readonly string[] FlatFiles = { GameSetFormat.BlocksFile, GameSetFormat.ItemsFile, GameSetFormat.CraftingFile, GameSetFormat.RecipesFile };

    public static bool HasFlatFiles(string root)
    {
        foreach (var file in FlatFiles)
        {
            if (File.Exists(Path.Combine(root, file))) return true;
        }
        return false;
    }

    public static ConversionResult Convert(string root, IEnumerable<string> protectedEmissionNames = null)
    {
        var result = new ConversionResult();
        root = Path.GetFullPath(root).TrimEnd('\\', '/');

        if (!Directory.Exists(root))
        {
            result.Errors.Add($"GameSet folder '{root}' was not found");
            return result;
        }

        if (!HasFlatFiles(root))
        {
            result.Log.Add("Nothing to convert: the GameSet has no flat content files");
            return result;
        }

        string parent = Path.GetDirectoryName(root);
        string name = Path.GetFileName(root);
        string staging = Path.Combine(parent, "_" + name + ".converting");
        string backup = Path.Combine(parent, "_" + name + ".backup");

        if (Directory.Exists(staging)) Directory.Delete(staging, true);
        CopyDirectory(root, staging);

        try
        {
            var before = ContentSnapshot.Create(root);
            ConvertInPlace(staging, protectedEmissionNames, result);
            if (result.Errors.Count == 0) result.Differences.AddRange(ContentSnapshot.Differences(before, ContentSnapshot.Create(staging)));
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }

        if (!result.Success)
        {
            Directory.Delete(staging, true);
            result.Log.Add("Conversion failed; the GameSet was not changed");
            return result;
        }

        if (Directory.Exists(backup)) Directory.Delete(backup, true);
        Directory.Move(root, backup);
        Directory.Move(staging, root);
        Directory.Delete(backup, true);
        result.Log.Add($"Converted '{name}': {result.Summary}; loaded content is identical");
        return result;
    }

    public static void ConvertInPlace(string root, IEnumerable<string> protectedEmissionNames, ConversionResult result)
    {
        var before = GameSetContentReader.ReadFolder(root);
        var blockNames = TextureNames(Path.Combine(root, GameSetFormat.BlocksFolder));
        var itemNames = TextureNames(Path.Combine(root, GameSetFormat.ItemsFolder));
        if (protectedEmissionNames != null)
        {
            foreach (var extra in protectedEmissionNames)
            {
                blockNames.Add(extra);
                itemNames.Add(extra);
            }
        }

        var blocks = new EntryFolderWriter(root, EntryKind.Blocks, itemNames, result);
        blocks.Convert(Keys(before.Blocks));
        var items = new EntryFolderWriter(root, EntryKind.Items, blockNames, result);
        items.Convert(Keys(before.Items));
        if (result.Errors.Count > 0) return;

        result.Blocks = blocks.EntryDirectories.Count;
        result.Items = items.EntryDirectories.Count;

        var recipes = new RecipeFilesWriter(root, result);
        recipes.ConvertCrafting(id => blocks.EntryDirectories.TryGetValue(id, out var block) ? block : items.EntryDirectories.TryGetValue(id, out var item) ? item : null);
        recipes.ConvertRecipes();

        RemoveEmptyFolders(Path.Combine(root, GameSetFormat.BlocksFolder));
        RemoveEmptyFolders(Path.Combine(root, GameSetFormat.ItemsFolder));
        RemoveEmptyFolders(Path.Combine(root, GameSetFormat.EmissionsFolder));
    }

    public static void ExportFlatInPlace(string root)
    {
        var content = GameSetContentReader.ReadFolder(root);

        foreach (var kind in new[] { EntryKind.Blocks, EntryKind.Items })
        {
            string folder = Path.Combine(root, kind.Folder);
            foreach (var file in EntryFolderScanner.FindEntryFiles(folder, kind.EntryFile))
            {
                string directory = Path.GetDirectoryName(file);
                File.Delete(file);
                DeleteIfExists(Path.Combine(directory, GameSetFormat.BlueprintFile));
                DeleteIfExists(Path.Combine(directory, GameSetFormat.EntryRecipesFile));
            }
            DeleteIfExists(Path.Combine(folder, GameSetFormat.OrderFile));
        }

        DeleteFolder(Path.Combine(root, GameSetFormat.CraftingFolder));
        DeleteFolder(Path.Combine(root, GameSetFormat.RecipesFolder));

        ContentJson.WriteFile(Path.Combine(root, GameSetFormat.BlocksFile), content.BlocksJson());
        ContentJson.WriteFile(Path.Combine(root, GameSetFormat.ItemsFile), content.ItemsJson());
        ContentJson.WriteFile(Path.Combine(root, GameSetFormat.CraftingFile), content.CraftingJson());
        ContentJson.WriteFile(Path.Combine(root, GameSetFormat.RecipesFile), content.RecipesJson());
    }

    public static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), true);
    }

    private static List<string> Keys(IReadOnlyList<ContentEntry> entries)
    {
        var keys = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.Key)) keys.Add(entry.Key);
        }
        return keys;
    }

    private static HashSet<string> TextureNames(string folder)
    {
        return new HashSet<string>(AssetFolderScanner.Scan(folder, GameSetFormat.ImageExtensions, null, false).Keys, StringComparer.OrdinalIgnoreCase);
    }

    private static void RemoveEmptyFolders(string root)
    {
        if (!Directory.Exists(root)) return;
        foreach (var directory in Directory.GetDirectories(root)) RemoveEmptyFolders(directory);
        if (Directory.GetFileSystemEntries(root).Length == 0) Directory.Delete(root);
    }

    private static void DeleteIfExists(string file)
    {
        if (File.Exists(file)) File.Delete(file);
    }

    private static void DeleteFolder(string folder)
    {
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }
}
