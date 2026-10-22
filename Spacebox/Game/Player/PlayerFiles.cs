using Engine;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.Player;

public static class PlayerFiles
{
    public static string SafeName(string playerName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((playerName ?? "Player").Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    public static string PlayerPath(string worldFolder, string playerName)
    {
        return Path.Combine(worldFolder, WorldFileLayout.PlayersFolder, SafeName(playerName) + WorldFileLayout.PlayerExtension);
    }

    public static string LegacyPlayerPath(string worldFolder, string playerName)
    {
        return Path.Combine(worldFolder, WorldFileLayout.LegacyPlayersFolder, SafeName(playerName) + ".json");
    }

    public static string LocalPlayerPath(string worldFolder) => Path.Combine(worldFolder, WorldFileLayout.LocalPlayerFile);

    public static string LegacyLocalPlayerPath(string worldFolder) => Path.Combine(worldFolder, WorldFileLayout.LegacyLocalPlayerFile);

    public static IReadOnlyList<string> SavedPlayerNames(string worldFolder)
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        AddNames(names, Path.Combine(worldFolder, WorldFileLayout.PlayersFolder), "*" + WorldFileLayout.PlayerExtension);
        AddNames(names, Path.Combine(worldFolder, WorldFileLayout.LegacyPlayersFolder), "*.json");
        return names.ToList();
    }

    public static DateTime LastSaved(string worldFolder, string playerName)
    {
        string path = PlayerPath(worldFolder, playerName);
        if (!File.Exists(path)) path = LegacyPlayerPath(worldFolder, playerName);
        return File.GetLastWriteTime(path);
    }

    public static void MoveLegacyFolder(string worldFolder)
    {
        if (string.IsNullOrEmpty(worldFolder) || !Directory.Exists(worldFolder)) return;

        string legacy = ExactChild(worldFolder, WorldFileLayout.LegacyPlayersFolder);
        if (legacy == null) return;

        try
        {
            string target = Path.Combine(worldFolder, WorldFileLayout.PlayersFolder);
            if (ExactChild(worldFolder, WorldFileLayout.PlayersFolder) == null)
            {
                string temp = Path.Combine(worldFolder, WorldFileLayout.PlayersFolder + "_moving");
                Directory.Move(legacy, temp);
                Directory.Move(temp, target);
                return;
            }

            foreach (var file in Directory.GetFiles(legacy))
            {
                string destination = Path.Combine(target, Path.GetFileName(file));
                if (!File.Exists(destination)) File.Move(file, destination);
            }
            if (Directory.GetFileSystemEntries(legacy).Length == 0) Directory.Delete(legacy);
        }
        catch (Exception ex)
        {
            Debug.Warning($"[Save] Could not rename the players folder in {worldFolder}: {ex.Message}");
        }
    }

    private static string ExactChild(string folder, string name)
    {
        foreach (var directory in Directory.GetDirectories(folder))
        {
            if (string.Equals(Path.GetFileName(directory), name, StringComparison.Ordinal)) return directory;
        }
        return null;
    }

    private static void AddNames(SortedSet<string> names, string folder, string pattern)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var file in Directory.EnumerateFiles(folder, pattern)) names.Add(Path.GetFileNameWithoutExtension(file));
    }
}
