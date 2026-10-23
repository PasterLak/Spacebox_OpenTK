using Engine;
using Engine.SceneManagement;
using Engine.Utils;
using Spacebox.Game.Resource;

namespace Spacebox.Editor;

public static class GameSetEditorLauncher
{
    public const string TexturePackStartPanel = "Textures";

    public static GameSetEditorArgs TexturePackArgs(string packFolder)
    {
        string baseId = Spacebox.Game.Globals.GameSet.Default.ToLowerInvariant();
        return new GameSetEditorArgs
        {
            ModId = baseId,
            ModFolderName = Spacebox.Network.GameSetLocator.FindFolder(baseId),
            TexturePack = packFolder,
            StartPanel = TexturePackStartPanel
        };
    }

    public static bool TryOpen(string gameSetFolder, string startPanel)
    {
        var pack = FindTexturePack(gameSetFolder);
        if (pack != null && InstalledGameSetFolder(gameSetFolder) == null)
        {
            var args = TexturePackArgs(pack.Folder);
            if (!string.IsNullOrEmpty(startPanel)) args.StartPanel = startPanel;
            SceneManager.Load<GameSetEditorScene, GameSetEditorArgs>(args);
            return true;
        }

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
        return InstalledGameSetFolder(idOrFolder) ?? idOrFolder;
    }

    private static string InstalledGameSetFolder(string idOrFolder)
    {
        var installed = Spacebox.Game.Resource.Content.InstalledGameSets.Scan(Spacebox.Game.Resource.Content.ModFolders.Local);
        return Spacebox.Game.Resource.Content.InstalledGameSets.Find(installed, idOrFolder)?.FolderName;
    }

    private static Spacebox.Game.Resource.TexturePacks.TexturePack FindTexturePack(string idOrFolder)
    {
        foreach (var pack in Spacebox.Game.Resource.TexturePacks.TexturePackService.Available)
        {
            if (pack.Matches(idOrFolder)) return pack;
        }
        return null;
    }
}
