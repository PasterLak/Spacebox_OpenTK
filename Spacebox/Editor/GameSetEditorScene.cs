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
}

public sealed class GameSetEditorScene : Scene, ISceneWithArgs<GameSetEditorArgs>
{
    private GameSetEditorArgs _args;
    private EditorContext _context;
    private Spacebox.Editor.Canvas.EditorHost _host;
    private Vector3 _previousAmbient;

    public void Initialize(GameSetEditorArgs param)
    {
        _args = param;
    }

    public override void LoadContent()
    {
        AddChild(new CameraStatic(Vector3.Zero));
        _previousAmbient = Lighting.AmbientColor;

        LoadGameSet();

        var targets = EditorTargets.For(_args.ModId);
        var project = new GameSetProject(EditorTargets.ProjectRoot(Spacebox.Network.GameSetLocator.FolderPath(_args.ModFolderName), _args.TargetBase), _args.ModId);
        project.Mirror = SourceFolderMirror.TryCreate(project.RootPath);
        if (project.Mirror != null) Engine.Debug.Log($"[Editor] Development build: saves also go to {project.Mirror.Root}");
        _context = new EditorContext(project) { Targets = targets, CurrentTarget = _args.TargetBase ?? "" };
        _context.Assets.Refresh();
        _host = new Spacebox.Editor.Canvas.EditorHost(_context, Spacebox.Editor.Canvas.EditorPages.CreateDefault(project), _args.StartPanel);
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
        if (_context == null) return;

        _context.Update(Time.Delta);
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
        _host?.Render();
        ColorOverlay.OnGUI();
    }

    public override void UnloadContent()
    {
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
        SceneAssetsPreloader.Preload(new SpaceSceneArgs
        {
            modId = targeted ? _args.TargetBase : _args.ModId,
            extensions = targeted ? new[] { _args.ModId } : Array.Empty<string>(),
            applyTexturePacks = false,
            modfolderName = targeted ? null : _args.ModFolderName,
            SearchForGameSetInLocalFolder = true
        });
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
