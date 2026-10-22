using Engine;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;
using Spacebox.Network;
using Spacebox.Scenes;

namespace Spacebox.Game.ShipViewer;

public sealed class ShipViewerGameSet
{
    private const string DefaultModId = "default";
    private const string DefaultFolder = "Default";

    public string LoadedModId { get; private set; }
    private string _loadedExtensions = "";
    public string Notice { get; private set; } = "";
    public int Version { get; private set; }
    public bool IsLoaded => LoadedModId != null && GameAssets.IsInitialized;

    public bool IsLoadedFor(string modId, IReadOnlyList<string> extensions = null)
    {
        return IsLoaded && string.Equals(LoadedModId, modId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(_loadedExtensions, WorldExtensionList.Format(extensions), StringComparison.OrdinalIgnoreCase);
    }

    public bool Load(string modId, IReadOnlyList<string> extensions = null)
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
                extensions = found && extensions != null ? new List<string>(extensions).ToArray() : Array.Empty<string>(),
                SearchForGameSetInLocalFolder = true
            });
            LoadedModId = modId;
            _loadedExtensions = WorldExtensionList.Format(extensions);
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
