using Spacebox.Game.Resource.Content.Conversion;

namespace Spacebox.GameSetTool;

public static class Program
{
    private const string Usage =
        "Usage:\n" +
        "  Spacebox.GameSetTool convert <gameSetFolder>                 convert flat blocks/items/crafting/recipes JSON to the folder format\n" +
        "  Spacebox.GameSetTool export-flat <gameSetFolder> <output>    copy a GameSet and write its content as flat JSON files\n" +
        "  Spacebox.GameSetTool snapshot <gameSetFolder> <file.json>    write the loaded content snapshot\n" +
        "  Spacebox.GameSetTool check <gameSetFolder> <file.json>       compare the loaded content with a snapshot";

    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string folder = Path.GetFullPath(args[1]);
        if (!Directory.Exists(folder))
        {
            Console.Error.WriteLine($"GameSet folder '{folder}' was not found.");
            return 2;
        }

        switch (args[0])
        {
            case "convert": return Convert(folder);
            case "export-flat" when args.Length >= 3: return ExportFlat(folder, Path.GetFullPath(args[2]));
            case "snapshot" when args.Length >= 3: return WriteSnapshot(folder, Path.GetFullPath(args[2]));
            case "check" when args.Length >= 3: return Check(folder, Path.GetFullPath(args[2]));
            default:
                Console.Error.WriteLine(Usage);
                return 2;
        }
    }

    private static int Convert(string folder)
    {
        var result = GameSetFolderConverter.Convert(folder, BuiltInTextureNames(folder));
        foreach (var line in result.Log) Console.WriteLine(line);
        foreach (var error in result.Errors) Console.Error.WriteLine("Error: " + error);
        foreach (var difference in result.Differences) Console.Error.WriteLine("Changed: " + difference);
        return result.Success ? 0 : 1;
    }

    private static int ExportFlat(string folder, string output)
    {
        if (Directory.Exists(output) && Directory.GetFileSystemEntries(output).Length > 0)
        {
            Console.Error.WriteLine($"'{output}' is not empty.");
            return 2;
        }

        GameSetFolderConverter.CopyDirectory(folder, output);
        GameSetFolderConverter.ExportFlatInPlace(output);
        Console.WriteLine($"Flat copy written to {output}");
        return 0;
    }

    private static int WriteSnapshot(string folder, string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllText(file, ContentSnapshot.ToText(ContentSnapshot.Create(folder)));
        Console.WriteLine($"Snapshot written to {file}");
        return 0;
    }

    private static int Check(string folder, string file)
    {
        var expected = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file)) as System.Text.Json.Nodes.JsonObject;
        var differences = ContentSnapshot.Differences(expected, ContentSnapshot.Create(folder));
        foreach (var difference in differences) Console.Error.WriteLine("Changed: " + difference);
        Console.WriteLine(differences.Count == 0 ? "Content is identical to the snapshot." : $"{differences.Count} differences.");
        return differences.Count == 0 ? 0 : 1;
    }

    private static IEnumerable<string> BuiltInTextureNames(string gameSetFolder)
    {
        var gameRoot = Directory.GetParent(gameSetFolder)?.Parent?.FullName;
        if (gameRoot == null) return Array.Empty<string>();

        var names = new List<string>();
        foreach (var relative in new[] { "Resources/Textures/Blocks", "Resources/Textures/Items" })
        {
            string path = Path.Combine(gameRoot, relative);
            if (!Directory.Exists(path)) continue;
            foreach (var file in Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
                names.Add(Path.GetFileNameWithoutExtension(file).ToLowerInvariant());
        }
        return names;
    }
}
