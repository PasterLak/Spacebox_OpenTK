using Engine.Utils;
using System.Text.Json;

using Engine;
namespace Spacebox.Game.GUI;

public class WorldInfoSaver
{
    public static void LoadWorlds(List<WorldInfo> worlds)
    {
        worlds.Clear();

        string worldsDirectory = GetWorldsDirectory();

        EnsureDirectoryExists(worldsDirectory);

        Dictionary<string, WorldInfo> worldPreviews = new Dictionary<string, WorldInfo>();

        foreach (string worldFolder in Directory.GetDirectories(worldsDirectory))
        {
            if (!WorldInfoFiles.TryRead(worldFolder, out var worldInfo))
                continue;

            if (string.IsNullOrEmpty(worldInfo.FolderName))
            {
                worldInfo.FolderName = Path.GetFileName(worldFolder);
                Save(worldInfo);
            }
            else if (worldInfo.FolderName != Path.GetFileName(worldFolder))
            {
                worldInfo.FolderName = Path.GetFileName(worldFolder);
            }

            worlds.Add(worldInfo);

            var pathPreview = Path.Combine(worldFolder, "preview.jpg");

            if (File.Exists(pathPreview))
            {
                worldPreviews.Add(pathPreview, worldInfo);
            }

        }

        if (worldPreviews.Count > 0)
        {

            var tasks = new List<Task>();

            foreach (var worldPreview in worldPreviews)
            {

                tasks.Add(Task.Run(() =>
                {
                    WorldInfo.LoadWorldPreview(worldPreview.Key, worldPreview.Value);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var worldPreview2 in worldPreviews)
            {
                WorldInfo.CreatePreviewFromPixels(worldPreview2.Value);
            }
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
        worldInfo.UpdateEditDate();
        WorldInfoFiles.Write(worldFolder, worldInfo);
    }

    public static void MarkOpened(WorldInfo worldInfo)
    {
        if (worldInfo == null) return;
        worldInfo.LastOpenedDate = WorldInfo.GetCurrentDate();

        string folder = GetWorldFolder(worldInfo);
        if (folder != null) WorldInfoFiles.Write(folder, worldInfo);
    }

    public static string GetWorldFolder(WorldInfo worldInfo)
    {
        if (worldInfo == null || string.IsNullOrWhiteSpace(worldInfo.FolderName)) return null;

        string folder = Path.Combine(GetWorldsDirectory(), worldInfo.FolderName);
        return Directory.Exists(folder) ? folder : null;
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