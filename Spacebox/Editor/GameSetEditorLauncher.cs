using Engine;
using Engine.SceneManagement;
using Engine.Utils;
using Spacebox.Game.Resource;

namespace Spacebox.Editor;

public static class GameSetEditorLauncher
{
    public static bool TryOpen(string gameSetFolder, string startPanel)
    {
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
}
