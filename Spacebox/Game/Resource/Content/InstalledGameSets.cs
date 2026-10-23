using Engine.Utils;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Game.Resource.Content;

public static class InstalledGameSets
{
    public static List<InstalledGameSet> Scan(ModFolders folders)
    {
        var result = new List<InstalledGameSet>();
        foreach (var folder in folders.ContentFolders) result.AddRange(Scan(folder));
        foreach (var duplicate in DuplicateIds(result))
            Engine.Debug.Warning($"[GameSets] Several GameSets use the id '{duplicate}', only the first one is used");
        return result;
    }

    public static List<string> DuplicateIds(IReadOnlyList<InstalledGameSet> gameSets)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<string>();
        foreach (var gameSet in gameSets)
        {
            if (!seen.Add(gameSet.Id) && !duplicates.Contains(gameSet.Id, StringComparer.OrdinalIgnoreCase)) duplicates.Add(gameSet.Id);
        }
        return duplicates;
    }

    private static bool HasProperty(System.Text.Json.Nodes.JsonNode node, string name)
    {
        if (node is not System.Text.Json.Nodes.JsonObject obj) return false;
        foreach (var pair in obj)
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static List<InstalledGameSet> Scan(string modsDirectory)
    {
        var result = new List<InstalledGameSet>();
        if (string.IsNullOrEmpty(modsDirectory) || !Directory.Exists(modsDirectory)) return result;

        var folders = Directory.GetDirectories(modsDirectory);
        Array.Sort(folders, StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders)
        {
            if (ModFolderInstaller.IsTransient(folder)) continue;
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
            if (!HasProperty(node, "ModId") || string.IsNullOrWhiteSpace(config.ModId)) config.ModId = config.FolderName.ToLowerInvariant();
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
        var graph = new ExtensionGraph(gameSets);

        foreach (var gameSet in gameSets)
        {
            if (gameSet.IsExtension && graph.Supports(gameSet, baseSet)) result.Add(gameSet);
        }
        return result;
    }
}
