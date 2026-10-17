using System.Text.Json;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Network;

public static class GameSetLocator
{
    private const string DefaultFolder = "Default";

    public static string FindFolder(string modId)
    {
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameSets");
        if (!Directory.Exists(directory)) return DefaultFolder;

        foreach (var folder in Directory.GetDirectories(directory))
        {
            string configPath = Path.Combine(folder, "config.json");
            if (!File.Exists(configPath)) continue;

            try
            {
                var config = JsonSerializer.Deserialize<ModConfig>(File.ReadAllText(configPath));
                if (config != null && string.Equals(config.ModId, modId, StringComparison.OrdinalIgnoreCase))
                    return Path.GetFileName(folder);
            }
            catch (JsonException)
            {
            }
        }

        return DefaultFolder;
    }
}
