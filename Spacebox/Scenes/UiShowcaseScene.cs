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
    private readonly List<IInterfaceVariant> _variants = new List<IInterfaceVariant> { new ClassicVariant(), new TutorialVariant(), new CompactVariant() };
    private readonly List<Button> _tabs = new List<Button>();
    private CanvasLayer _layer;
    private ShowcaseAssets _assets;
    private ShowcaseState _state;
    private ShowcaseBindings _bindings;
    private Control _current;
    private Control _overlay;
    private Label _description;
    private Label _stats;
    private int _index;
    private float _statsTimer;

    public override void LoadContent()
    {
        AddChild(new CameraStatic(Vector3.Zero));
        Lighting.Skybox = new Skybox(new SpaceTexture(512, 512, World.Seed));

        _assets = new ShowcaseAssets();
        _state = new ShowcaseState();
        _state.Fill(_assets);
        _layer = new CanvasLayer();
        _layer.Root.VirtualSize = new Vector2(1280f, 720f);
        _layer.Root.StretchMode = StretchMode.Expand;

        BuildOverlay();
        Show(0);
    }

    public override void Update()
    {
        base.Update();
        if (Input.IsKeyDown(Keys.Escape))
        {
            SceneManager.Load<MenuScene>();
            return;
        }

        for (int i = 0; i < _variants.Count; i++)
            if (Input.IsKeyDown(Keys.D1 + i) || Input.IsKeyDown(Keys.F1 + i)) Show(i);
        if (Input.IsKeyDown(Keys.Tab) || Input.IsKeyDown(Keys.I)) _bindings.SetWindowsVisible(!_bindings.WindowsVisible);

        float scroll = Input.Mouse.ScrollDelta.Y;
        if (scroll != 0f)
            _state.SelectedHotbar = (_state.SelectedHotbar - Math.Sign(scroll) + ShowcaseState.HotbarSize) % ShowcaseState.HotbarSize;

        _state.Update(Time.Delta);
        _bindings.Update(_state);
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

    private void Show(int index)
    {
        _index = index;
        bool windows = _bindings?.WindowsVisible ?? true;
        if (_current != null) _layer.Root.Content.RemoveChild(_current);

        var variant = _variants[index];
        _bindings = new ShowcaseBindings();
        _current = variant.Build(_assets, _state, _bindings);
        _layer.Root.Content.AddChild(_current);
        _layer.Root.Theme = _current.Theme;
        _bindings.SetWindowsVisible(windows);
        _overlay.MoveToFront();

        _description.Text = variant.Description;
        for (int i = 0; i < _tabs.Count; i++) _tabs[i].ButtonPressed = i == index;
    }

    private void BuildOverlay()
    {
        _overlay = _layer.Root.Add(new PanelContainer { Theme = OverlayTheme(), ThemeTypeVariation = "Bar" });
        _overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide, 0f, new Vector2(0f, 64f));

        var column = _overlay.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 4f);
        var row = column.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 6f);
        row.AddChild(ShowcaseParts.Text("UI SHOWCASE", ShowcaseParts.Rgba(255, 196, 40), 24f));
        row.AddChild(ShowcaseParts.Spacer(12f));

        for (int i = 0; i < _variants.Count; i++)
        {
            int index = i;
            var tab = row.AddChild(new Button($"{i + 1}  {_variants[i].Name}") { ToggleMode = true });
            tab.Pressed += _ => Show(index);
            _tabs.Add(tab);
        }

        row.AddChild(ShowcaseParts.Spacer(0f, 0f, true));
        var toggle = row.AddChild(new Button("Tab  Windows"));
        toggle.Pressed += _ => _bindings.SetWindowsVisible(!_bindings.WindowsVisible);
        var back = row.AddChild(new Button("Esc  Menu"));
        back.Pressed += _ => SceneManager.Load<MenuScene>();

        var info = column.AddChild(new HBoxContainer());
        _description = info.AddChild(ShowcaseParts.Text("", ShowcaseParts.Rgba(185, 190, 200), 12f));
        _description.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _stats = info.AddChild(ShowcaseParts.Text("", ShowcaseParts.Rgba(130, 135, 145), 12f, HorizontalAlignment.Right));
    }

    private Theme OverlayTheme()
    {
        var gold = ShowcaseParts.Rgba(255, 196, 40);
        var border = ShowcaseParts.Rgba(90, 100, 120);
        var theme = new Theme { DefaultFont = _assets.Font, DefaultFontSize = 12f };
        theme.SetStyleBox("Bar", "panel", new StyleBoxFlat(ShowcaseParts.Rgba(7, 9, 14, 220), 10f) { BorderBottom = 1f, BorderColor = border });
        theme.SetStyleBox("Button", "normal", Box(ShowcaseParts.Rgba(45, 50, 62), border));
        theme.SetStyleBox("Button", "hover", Box(ShowcaseParts.Rgba(58, 64, 80), ShowcaseParts.Rgba(170, 175, 185)));
        theme.SetStyleBox("Button", "pressed", Box(ShowcaseParts.Rgba(58, 52, 30), gold));
        theme.SetColor("Button", "font_color", ShowcaseParts.Rgba(240, 240, 240));
        return theme;
    }

    private static StyleBoxFlat Box(Color4 background, Color4 border)
    {
        var box = new StyleBoxFlat(background).SetBorder(1f, border);
        box.SetContentMargins(8f, 4f);
        return box;
    }

    private void UpdateStats()
    {
        _statsTimer -= Time.Delta;
        if (_statsTimer > 0f) return;
        _statsTimer = 0.5f;
        var renderer = _layer.Renderer;
        var root = _layer.Root;
        _stats.Text = $"{renderer.DrawCalls} draw calls · {renderer.Quads} quads · virtual {root.ViewportSize.X:0}x{root.ViewportSize.Y:0} · scale {root.Scale:0.00}";
    }
}
