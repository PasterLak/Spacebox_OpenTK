using Engine;
using Engine.Light;
using Engine.SceneManagement;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Scenes;

namespace Spacebox.Editor;

public struct GameSetEditorArgs
{
    public string ModId;
    public string ModFolderName;
    public string StartPanel;
    public string TargetBase;
    public string TexturePack;
}

public sealed class GameSetEditorScene : Scene, ISceneWithArgs<GameSetEditorArgs>
{
    private GameSetEditorArgs _args;
    private EditorContext _context;
    private Spacebox.Editor.Canvas.EditorHost _host;
    private readonly Spacebox.Game.GUI.Canvas.Screens.FadeOverlayView _fade = new Spacebox.Game.GUI.Canvas.Screens.FadeOverlayView();
    private Vector3 _previousAmbient;
    private Spacebox.Game.Resource.Content.GameSetAssetIndex _baseTextures;
    private StagedLoader _loader;
    private Spacebox.Editor.Canvas.EditorLoadingScreen _loading;
    private GameSetProject _project;

    private bool IsTexturePack => !string.IsNullOrEmpty(_args.TexturePack);

    public void Initialize(GameSetEditorArgs param)
    {
        _args = param;
    }

    public override void LoadContent()
    {
        AddChild(new CameraStatic(Vector3.Zero));
        _previousAmbient = Lighting.AmbientColor;
        Input.ShowCursor();

        _loading = new Spacebox.Editor.Canvas.EditorLoadingScreen();
        _loader = new StagedLoader()
            .Add("Loading the GameSet", LoadGameSet)
            .Add("Opening the project", OpenProject)
            .Add("Building the editor pages", BuildHost);
        _loading.Show(_loader.CurrentStep, 0f);
    }

    private void OpenProject()
    {
        var project = IsTexturePack
            ? new GameSetProject(_args.TexturePack, Spacebox.Game.Resource.TexturePacks.TexturePack.Load(_args.TexturePack)?.Id ?? "")
            : new GameSetProject(EditorTargets.ProjectRoot(Spacebox.Network.GameSetLocator.FolderPath(_args.ModFolderName), _args.TargetBase), _args.ModId);
        project.Mirror = SourceFolderMirror.TryCreate(project.RootPath);
        if (project.Mirror != null) Engine.Debug.Log($"[Editor] Development build: saves also go to {project.Mirror.Root}");
        _context = IsTexturePack
            ? new EditorContext(project) { TexturePackFolder = _args.TexturePack, TexturePackBase = _args.ModId }
            : new EditorContext(project) { Targets = EditorTargets.For(_args.ModId), CurrentTarget = _args.TargetBase ?? "" };
        _context.BaseTextures = _baseTextures;
        _context.Assets.Refresh();
        _project = project;
    }

    private void BuildHost()
    {
        var pages = IsTexturePack ? Spacebox.Editor.Canvas.EditorPages.CreateTexturePack() : Spacebox.Editor.Canvas.EditorPages.CreateDefault(_project);
        _host = new Spacebox.Editor.Canvas.EditorHost(_context, pages, _args.StartPanel);
        _host.Root.Add(_fade, Engine.CanvasUI.UiLayer.Tooltip);
        Input.ShowCursor();
    }

    public override void Start()
    {
        base.Start();
        ColorOverlay.FadeOut(new System.Numerics.Vector3(0f, 0f, 0f), 0.5f);
    }

    public override void Update()
    {
        base.Update();
        if (_loader != null && !_loader.IsDone)
        {
            _loader.Tick();
            _loading?.Show(_loader.CurrentStep, _loader.Progress);
            _loading?.Update(Time.Delta);
            if (!_loader.IsDone) return;

            _loading?.Dispose();
            _loading = null;
            ColorOverlay.FadeOut(new System.Numerics.Vector3(0f, 0f, 0f), 1f, 0.4f);
        }
        if (_context == null || _host == null) return;

        _context.Update(Time.Delta);
        ColorOverlay.Update();
        _fade.Sync(Time.Delta);
        _host?.Update(Time.Delta);

        if (_context.ReloadRequested)
        {
            _context.ClearReloadRequest();
            ReloadGameSet();
        }

        if (_context.ExitRequested) SceneManager.Load<MenuScene>();
        else if (_context.TargetRequest != null)
        {
            var args = _args;
            args.TargetBase = _context.TargetRequest;
            args.StartPanel = _host?.CurrentPageTitle ?? _args.StartPanel;
            _context.ClearTargetRequest();
            SceneManager.Load<GameSetEditorScene, GameSetEditorArgs>(args);
        }
    }

    public override void Render()
    {
        base.Render();
        _context?.RenderPreviews();
    }

    public override void OnGUI()
    {
        if (_loading != null) _loading.Render();
        else _host?.Render();
    }

    public override void UnloadContent()
    {
        _loading?.Dispose();
        _loading = null;
        _host?.Dispose();
        _host = null;
        _context?.Dispose();
        _context = null;
        GameAssets.DisposeAll();
        Lighting.AmbientColor = _previousAmbient;
    }

    private void LoadGameSet()
    {
        bool targeted = !string.IsNullOrEmpty(_args.TargetBase);
        var pack = IsTexturePack ? Spacebox.Game.Resource.TexturePacks.TexturePack.Load(_args.TexturePack) : null;
        var stack = SceneAssetsPreloader.Preload(new SpaceSceneArgs
        {
            modId = targeted ? _args.TargetBase : _args.ModId,
            extensions = targeted ? new[] { _args.ModId } : Array.Empty<string>(),
            applyTexturePacks = false,
            textureOverrides = pack != null ? new Spacebox.Game.Resource.Content.ITextureOverrideSource[] { pack } : null,
            modfolderName = targeted ? null : _args.ModFolderName,
            SearchForGameSetInLocalFolder = true
        });
        _baseTextures = IsTexturePack
            ? Spacebox.Game.Resource.Content.GameSetAssetIndex.Build(stack?.AssetLayers ?? Array.Empty<Spacebox.Game.Resource.Content.ContentLayer>(), Spacebox.Game.Resource.GameAtlasFactory.BuiltInBlocksFolder, Spacebox.Game.Resource.GameAtlasFactory.BuiltInItemsFolder)
            : null;
        if (_context != null) _context.BaseTextures = _baseTextures;
    }

    private void ReloadGameSet()
    {
        try
        {
            _context.InvalidatePreviews();
            LoadGameSet();
            _context.Assets.Refresh();
            _context.SetStatus("GameSet reloaded");
        }
        catch (Exception ex)
        {
            Engine.Debug.Error($"[Editor] Reload failed: {ex}");
            _context.SetStatus("Reload failed: " + ex.Message, true);
        }
    }
}
