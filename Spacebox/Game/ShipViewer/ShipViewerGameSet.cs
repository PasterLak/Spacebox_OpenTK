using Engine;
using Spacebox.Game.Resource;
using Spacebox.Network;
using Spacebox.Scenes;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipViewerGameSet
{
    private const string DefaultModId = "default";
    private const string DefaultFolder = "Default";

    public string LoadedModId { get; private set; }
    public string Notice { get; private set; } = "";
    public int Version { get; private set; }
    public bool IsLoaded => LoadedModId != null && GameAssets.IsInitialized;

    public bool IsLoadedFor(string modId)
    {
        return IsLoaded && string.Equals(LoadedModId, modId, StringComparison.OrdinalIgnoreCase);
    }

    public bool Load(string modId)
    {
        Release();

        bool found = GameSetLocator.TryFindFolderPath(GameSetLocator.LocalDirectory, modId, out var path);
        string loadId = found ? modId : DefaultModId;
        string folder = found ? Path.GetFileName(path) : DefaultFolder;
        Notice = found ? "" : $"GameSet '{modId}' is not installed, showing with the Default GameSet";

        try
        {
            SceneAssetsPreloader.Preload(new SpaceSceneArgs
            {
                modId = loadId,
                modfolderName = folder,
                SearchForGameSetInLocalFolder = true
            });
            LoadedModId = modId;
            Version++;
            return true;
        }
        catch (Exception ex)
        {
            Notice = $"GameSet '{modId}' could not be loaded: {ex.Message}";
            Debug.Error($"[ShipViewer] {Notice}");
            GameAssets.DisposeAll();
            LoadedModId = null;
            return false;
        }
    }

    public void Release()
    {
        if (LoadedModId != null && GameAssets.IsInitialized) GameAssets.DisposeAll();
        LoadedModId = null;
    }
}
