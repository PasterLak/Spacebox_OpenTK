using Microsoft.VisualBasic;
using Spacebox.Game.GUI;
using System;
using System.IO;
using System.Text.Json;

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
                    Console.WriteLine($"[ERROR] Directory Worlds was not founded!: {WorldsDirectory}");
                    return null;
                }

                // Получаем все подпапки в директории Worlds
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
                            // Найдено соответствие по названию мира
                            LoadedWorld loadedWorld = new LoadedWorld
                            {
                                Info = worldInfo,
                                WorldFolderPath = worldFolder
                            };

                            Console.WriteLine($"[SUCCESS] Мир '{worldName}' успешно загружен из '{worldFolder}'.");
                            return loadedWorld;
                        }
                    }
                }

                Console.WriteLine($"[ERROR] Мир '{worldName}' не найден в '{WorldsDirectory}'.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Произошла ошибка при загрузке мира '{worldName}': {ex.Message}");
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
