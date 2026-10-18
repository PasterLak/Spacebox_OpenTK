using System.Text.Json;
using Spacebox.Game;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Network;

public static class GameSetLocator
{
    private const string DefaultFolder = "Default";

    public static string LocalDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.GameSet.LocalFolder);

    public static string FindFolder(string modId)
    {
        return TryFindFolderPath(LocalDirectory, modId, out var path) ? Path.GetFileName(path) : DefaultFolder;
    }

    public static bool TryFindFolderPath(string modsDirectory, string modId, out string path)
    {
        path = null;
        if (!Directory.Exists(modsDirectory)) return false;

        foreach (var folder in Directory.GetDirectories(modsDirectory))
        {
            string configPath = Path.Combine(folder, "config.json");
            if (!File.Exists(configPath)) continue;

            try
            {
                var config = JsonSerializer.Deserialize<ModConfig>(File.ReadAllText(configPath));
                if (config != null && string.Equals(config.ModId, modId, StringComparison.OrdinalIgnoreCase))
                {
                    path = folder;
                    return true;
                }
            }
            catch (JsonException)
            {
            }
        }

        return false;
    }
}
