using Engine;
using Engine.Light;
using Engine.SceneManagement;
using ImGuiNET;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Editor.UI;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Scenes;

namespace Spacebox.Editor;

public struct GameSetEditorArgs
{
    public string ModId;
    public string ModFolderName;
    public string StartPanel;
}

public sealed class GameSetEditorScene : Scene, ISceneWithArgs<GameSetEditorArgs>
{
    private GameSetEditorArgs _args;
    private EditorContext _context;
    private EditorShell _shell;
    private float _previousFontScale = 1f;
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

        var modsFolder = ModPath.GetModsPath(true, "");
        var project = new GameSetProject(Path.Combine(modsFolder, _args.ModFolderName), _args.ModId);
        _context = new EditorContext(project);
        _context.Assets.Refresh();
        _shell = new EditorShell(_context, EditorPanelFactory.CreateDefault(project), _args.StartPanel);

        _previousFontScale = ImGui.GetIO().FontGlobalScale;
        Input.ShowCursor();
    }

    public override void Start()
    {
        base.Start();
        MenuScene.ResetImGuiFont();
        ColorOverlay.FadeOut(new System.Numerics.Vector3(0f, 0f, 0f), 0.5f);
    }

    public override void Update()
    {
        base.Update();
        if (_context == null) return;

        _context.Update(Time.Delta);

        if (_context.ReloadRequested)
        {
            _context.ClearReloadRequest();
            ReloadGameSet();
        }

        if (_context.ExitRequested) SceneManager.Load<MenuScene>();
    }

    public override void Render()
    {
        base.Render();
        _context?.RenderPreviews();
    }

    public override void OnGUI()
    {
        EditorTheme.Apply();
        ImGui.GetIO().FontGlobalScale = EditorWidgets.Scale;
        _shell?.Draw();
        ImGui.GetIO().FontGlobalScale = _previousFontScale;
        ColorOverlay.OnGUI();
    }

    public override void UnloadContent()
    {
        _shell?.Dispose();
        _context?.Dispose();
        _shell = null;
        _context = null;
        GameAssets.DisposeAll();
        Lighting.AmbientColor = _previousAmbient;
        ImGui.GetIO().FontGlobalScale = _previousFontScale;
    }

    private void LoadGameSet()
    {
        SceneAssetsPreloader.Preload(new SpaceSceneArgs
        {
            modId = _args.ModId,
            modfolderName = _args.ModFolderName,
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
