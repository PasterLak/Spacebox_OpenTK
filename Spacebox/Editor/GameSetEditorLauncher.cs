using Engine;
using Engine.SceneManagement;
using Engine.Utils;
using Spacebox.Game.Resource;

namespace Spacebox.Editor;

public static class GameSetEditorLauncher
{
    public static bool TryOpen(string gameSetFolder, string startPanel)
    {
        gameSetFolder = FolderOf(gameSetFolder);
        var configPath = Path.Combine(Spacebox.Network.GameSetLocator.FolderPath(gameSetFolder), "config.json");
        if (!File.Exists(configPath))
        {
            Debug.Error($"[Editor] GameSet folder '{gameSetFolder}' has no config.json");
            return false;
        }

        var config = JsonFixer.LoadJsonSafe<GameSetLoader.ModConfig>(configPath);
        if (config == null) return false;

        SceneManager.Load<GameSetEditorScene, GameSetEditorArgs>(new GameSetEditorArgs
        {
            ModId = config.ModId,
            ModFolderName = gameSetFolder,
            StartPanel = startPanel
        });
        return true;
    }

    private static string FolderOf(string idOrFolder)
    {
        var installed = Spacebox.Game.Resource.Content.InstalledGameSets.Scan(Spacebox.Game.Resource.Content.ModFolders.Local);
        return Spacebox.Game.Resource.Content.InstalledGameSets.Find(installed, idOrFolder)?.FolderName ?? idOrFolder;
    }
}
