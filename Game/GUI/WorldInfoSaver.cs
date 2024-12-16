using System.Text.Json;
using Spacebox.Common;

namespace Spacebox.Game.GUI;

public class WorldInfoSaver
{
    public static void LoadWorlds(List<WorldInfo> worlds)
    {
        worlds.Clear();

        string worldsDirectory = GetWorldsDirectory();

        EnsureDirectoryExists(worldsDirectory);

        foreach (string worldFolder in Directory.GetDirectories(worldsDirectory))
        {
            string worldJsonPath = Path.Combine(worldFolder, "world.json");

            if (!File.Exists(worldJsonPath))
                continue;

            string jsonContent = File.ReadAllText(worldJsonPath);
            WorldInfo? worldInfo = JsonSerializer.Deserialize<WorldInfo>(jsonContent);

            if (worldInfo == null)
                continue;

            if (string.IsNullOrEmpty(worldInfo.FolderName))
            {
                worldInfo.FolderName = Path.GetFileName(worldFolder);
                Save(worldInfo);
            }

            worlds.Add(worldInfo);
        }
    }
    
    public static void Save(WorldInfo worldInfo)
    {
        string worldsDirectory = GetWorldsDirectory();

        if (!Directory.Exists(worldsDirectory))
        {
            Debug.Error($"Worlds directory not found: {worldsDirectory}");
            return;
        }

        string worldFolder = Path.Combine(worldsDirectory, worldInfo.FolderName);

        if (!Directory.Exists(worldFolder))
        {
            Debug.Error($"World folder not found for {worldInfo.Name}: {worldFolder}");
            return;
        }

        string worldJsonPath = Path.Combine(worldFolder, "world.json");
        string jsonContent = JsonSerializer.Serialize(worldInfo, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(worldJsonPath, jsonContent);
    }
    
    private static string GetWorldsDirectory()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.Menu.WorldsFolder);
    }

    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

}