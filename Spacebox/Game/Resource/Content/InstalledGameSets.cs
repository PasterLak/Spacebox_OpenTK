using Engine.Utils;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Game.Resource.Content;

public static class InstalledGameSets
{
    public static List<InstalledGameSet> Scan(string modsDirectory)
    {
        var result = new List<InstalledGameSet>();
        if (string.IsNullOrEmpty(modsDirectory) || !Directory.Exists(modsDirectory)) return result;

        var folders = Directory.GetDirectories(modsDirectory);
        Array.Sort(folders, StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders)
        {
            var config = ReadConfig(folder);
            if (config != null) result.Add(new InstalledGameSet(folder, config));
        }
        return result;
    }

    public static ModConfig ReadConfig(string folder)
    {
        string path = Path.Combine(folder, GameSetFormat.ConfigFile);
        if (!File.Exists(path)) return null;

        var node = ContentJson.ParseFile(path, null);
        if (node == null) return null;

        try
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<ModConfig>(node, ContentJson.CaseInsensitive);
            if (config == null) return null;
            config.Extends ??= "";
            config.Includes ??= new List<string>();
            config.FolderName = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return config;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public static InstalledGameSet Find(IReadOnlyList<InstalledGameSet> gameSets, string idOrFolder)
    {
        foreach (var gameSet in gameSets)
        {
            if (string.Equals(gameSet.Id, idOrFolder, StringComparison.OrdinalIgnoreCase)) return gameSet;
        }
        foreach (var gameSet in gameSets)
        {
            if (gameSet.Matches(idOrFolder)) return gameSet;
        }
        return null;
    }

    public static List<InstalledGameSet> ExtensionsFor(IReadOnlyList<InstalledGameSet> gameSets, InstalledGameSet baseSet)
    {
        var result = new List<InstalledGameSet>();
        if (baseSet == null) return result;

        foreach (var gameSet in gameSets)
        {
            if (gameSet.IsExtension && baseSet.Matches(gameSet.Config.Extends)) result.Add(gameSet);
        }
        return result;
    }
}
