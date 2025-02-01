using Spacebox.Game.GUI;
using System.Text.Json;
using Engine;
namespace Spacebox.Game
{
    public static class WorldLoader
    {
        private static readonly string WorldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");

        public static LoadedWorld LoadWorldByName(string worldName)
        {
            try
            {
                if (!Directory.Exists(WorldsDirectory))
                {
                    Console.WriteLine($"[ERROR] Directory Worlds was not found!: {WorldsDirectory}");
                    return null;
                }

                string[] worldFolders = Directory.GetDirectories(WorldsDirectory);

                foreach (string worldFolder in worldFolders)
                {
                    string worldJsonPath = Path.Combine(worldFolder, "world.json");
                    if (File.Exists(worldJsonPath))
                    {
                        string jsonContent = File.ReadAllText(worldJsonPath);
                        WorldInfo worldInfo = JsonSerializer.Deserialize<WorldInfo>(jsonContent);

                        if (worldInfo != null && string.Equals(worldInfo.Name, worldName, StringComparison.OrdinalIgnoreCase))
                        {
                            LoadedWorld loadedWorld = new LoadedWorld
                            {
                                Info = worldInfo,
                                WorldFolderPath = worldFolder
                            };

                            Console.WriteLine($"[SUCCESS] World '{worldName}' successfully loaded from '{worldFolder}'.");
                            return loadedWorld;
                        }
                    }
                }

                Console.WriteLine($"[ERROR] World '{worldName}' not found in '{WorldsDirectory}'.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] An error occurred while loading the world '{worldName}': {ex.Message}");
                return null;
            }
        }

        public class LoadedWorld
        {
            public WorldInfo Info { get; set; }
            public string WorldFolderPath { get; set; }

            public string GetName() => Info.Name;
        }
    }
}
