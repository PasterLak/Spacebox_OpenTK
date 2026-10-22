using Engine;
using Engine.CanvasUI;
using Engine.SceneManagement;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine.Light;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI.Showcase;

namespace Spacebox.Scenes;

public sealed class UiShowcaseScene : Scene
{
    private const float UiScaleStep = 0.25f;
    private const float MinUiScale = 0.5f;
    private const float MaxUiScale = 3f;

    private readonly IReadOnlyList<IInterfaceVariant> _variants = ShowcaseVariants.CreateAll();
    private CanvasLayer _layer;
    private ShowcaseAssets _assets;
    private ShowcaseHost _host;
    private float _statsTimer;

    public override void LoadContent()
    {
        AddChild(new CameraStatic(Vector3.Zero));
        Lighting.Skybox = new Skybox(new SpaceTexture(512, 512, World.Seed));

        _assets = ShowcaseAssets.Load();
        var state = new ShowcaseState();
        state.Fill(_assets);
        _layer = new CanvasLayer("UI Showcase");
        _host = new ShowcaseHost(_layer.Root, _assets, state, _variants);
        _host.BackRequested += () => SceneManager.Load<MenuScene>();
    }

    public override void Update()
    {
        base.Update();
        if (Input.IsKeyDown(Keys.Escape))
        {
            if (_host.HelpVisible) _host.CloseHelp();
            else SceneManager.Load<MenuScene>();
            return;
        }

        for (int i = 0; i < _variants.Count; i++)
            if (Input.IsKeyDown(Keys.D1 + i) || Input.IsKeyDown(Keys.F1 + i)) _host.Show(i);
        if (Input.IsKeyDown(Keys.Tab) || Input.IsKeyDown(Keys.I)) _host.ToggleWindows();
        if (Input.IsKeyDown(Keys.H)) _host.ToggleHelp();
        if (Input.IsKeyDown(Keys.F8)) _layer.ShowLayoutIssues = !_layer.ShowLayoutIssues;
        if (Input.IsKeyDown(Keys.Equal) || Input.IsKeyDown(Keys.KeyPadAdd)) ChangeUiScale(UiScaleStep);
        if (Input.IsKeyDown(Keys.Minus) || Input.IsKeyDown(Keys.KeyPadSubtract)) ChangeUiScale(-UiScaleStep);

        float scroll = Input.Mouse.ScrollDelta.Y;
        if (scroll != 0f && !_layer.Root.WantsPointer) _host.ScrollHotbar(scroll);

        _host.Update(Time.Delta);
        _layer.Update(Time.Delta);
        UpdateStats();
    }

    public override void OnGUI()
    {
        base.OnGUI();
        _layer?.Render();
    }

    public override void UnloadContent()
    {
        _layer?.Dispose();
        _assets?.Dispose();
    }

    private void ChangeUiScale(float delta)
    {
        var stretch = _layer.Root.Stretch;
        stretch.UiScale = Math.Clamp(stretch.UiScale + delta, MinUiScale, MaxUiScale);
    }

    private void UpdateStats()
    {
        _statsTimer -= Time.Delta;
        if (_statsTimer > 0f) return;
        _statsTimer = 0.5f;
        var renderer = _layer.Renderer;
        var root = _layer.Root;
        string issues = _layer.ShowLayoutIssues && _layer.LastReport != null ? $" · {_layer.LastReport.ErrorCount} layout issues" : "";
        _host.SetStats($"{renderer.DrawCalls} draw calls · {renderer.Quads} quads · {root.ViewportSize.X:0}x{root.ViewportSize.Y:0} · scale {root.Scale:0.00} (ui {root.Stretch.UiScale:0.##}){issues}");
    }
}
