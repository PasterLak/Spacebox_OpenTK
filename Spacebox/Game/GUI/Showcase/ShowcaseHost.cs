using Engine.CanvasUI;
using OpenTK.Mathematics;
using static Spacebox.Game.GUI.Showcase.ShowcaseParts;

namespace Spacebox.Game.GUI.Showcase;

public sealed class ShowcaseHost
{
    public static readonly Vector2 VirtualSize = new Vector2(1280f, 720f);
    public static readonly Vector2 MinimumViewport = new Vector2(1024f, 576f);
    public const float MinScale = 0.75f;
    public const float MaxSafeAspect = 21f / 9f;

    private readonly CanvasRoot _root;
    private readonly ShowcaseAssets _assets;
    private readonly ShowcaseState _state;
    private readonly IReadOnlyList<IInterfaceVariant> _variants;
    private readonly List<Button> _tabs = new List<Button>();
    private readonly ExclusionArea _hudArea;
    private readonly ExclusionArea _windowsArea;
    private readonly SafeArea _popupArea;
    private readonly ShowcaseHelp _help;
    private Label _description;
    private Label _stats;

    public ShowcaseBindings Bindings { get; private set; } = new ShowcaseBindings();
    public int Index { get; private set; }
    public IInterfaceVariant Current => _variants[Index];
    public PanelContainer Bar { get; }
    public IReadOnlyList<Button> Tabs => _tabs;
    public Control Hud => _hudArea;
    public Control Windows => _windowsArea;
    public Control Popups => _popupArea;
    public bool HelpVisible => _help.IsOpen;
    public event Action BackRequested;

    public ShowcaseHost(CanvasRoot root, ShowcaseAssets assets, ShowcaseState state, IReadOnlyList<IInterfaceVariant> variants)
    {
        _root = root;
        _assets = assets;
        _state = state;
        _variants = variants;
        Configure(root.Stretch);

        var theme = BarTheme(assets);
        var barArea = root.Add(new SafeArea { Name = "ShowcaseBarArea" }, UiLayer.Hud);
        Bar = barArea.AddChild(BuildBar(theme));

        _hudArea = root.Add(new ExclusionArea { Name = "HudArea" }, UiLayer.Hud);
        _hudArea.Exclude(Bar);
        _windowsArea = root.Add(new ExclusionArea { Name = "WindowsArea", Gap = 8f }, UiLayer.Windows);
        _windowsArea.Exclude(Bar).ExcludeChildrenOf(_hudArea);
        _popupArea = root.Add(new SafeArea { Name = "PopupArea" }, UiLayer.Popup);
        _help = new ShowcaseHelp(root, theme);

        Show(0);
    }

    public static void Configure(StretchSettings stretch)
    {
        stretch.VirtualSize = VirtualSize;
        stretch.MinimumViewportSize = MinimumViewport;
        stretch.Mode = StretchMode.Expand;
        stretch.MinScale = MinScale;
        stretch.MaxSafeAspect = MaxSafeAspect;
    }

    public void Show(int index)
    {
        Index = Math.Clamp(index, 0, _variants.Count - 1);
        bool windows = Bindings.WindowsVisible;
        _hudArea.ClearChildren();
        _windowsArea.ClearChildren();
        _popupArea.ClearChildren();

        var variant = _variants[Index];
        var theme = variant.CreateTheme(_assets);
        _hudArea.Theme = theme;
        _windowsArea.Theme = theme;
        _popupArea.Theme = theme;
        _root.Theme = theme;

        Bindings = new ShowcaseBindings();
        variant.Build(new ShowcaseView { Hud = _hudArea, Windows = _windowsArea, Popups = _popupArea, Assets = _assets, State = _state, Bindings = Bindings });
        Bindings.SetWindowsVisible(windows);
        Bindings.Update(_state);

        _description.Text = variant.Description;
        for (int i = 0; i < _tabs.Count; i++) _tabs[i].ButtonPressed = i == Index;
    }

    public void ToggleWindows() => Bindings.SetWindowsVisible(!Bindings.WindowsVisible);

    public void ToggleHelp()
    {
        if (_help.IsOpen) _help.Close();
        else _help.Open();
    }

    public void CloseHelp() => _help.Close();

    public void Update(float deltaTime)
    {
        _state.Update(deltaTime);
        Bindings.Update(_state);
    }

    public void SetStats(string text) => _stats.Text = text;

    public void ScrollHotbar(float delta)
    {
        int size = ShowcaseState.HotbarSize;
        _state.SelectedHotbar = (_state.SelectedHotbar - Math.Sign(delta) + size) % size;
    }

    private PanelContainer BuildBar(Engine.CanvasUI.Theme theme)
    {
        var bar = new PanelContainer { Name = "ShowcaseBar", Theme = theme, ThemeTypeVariation = "Bar" };
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);

        var column = bar.AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 4f);
        var row = column.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 6f);
        var title = row.AddChild(Text("UI SHOWCASE", Rgba(255, 196, 40), 24f));
        title.Overrun = TextOverrun.Ellipsis;
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        title.CustomMinimumSize = new Vector2(64f, 0f);

        for (int i = 0; i < _variants.Count; i++)
        {
            int index = i;
            var tab = row.AddChild(new Button($"{i + 1} {_variants[i].Name}") { ToggleMode = true, TooltipText = _variants[i].Description });
            tab.Pressed += _ => Show(index);
            _tabs.Add(tab);
        }

        row.AddChild(Spacer(6f));
        row.AddChild(new Button("Tab Windows") { TooltipText = "Show or hide the windows" }).Pressed += _ => ToggleWindows();
        row.AddChild(new Button("? Help") { TooltipText = "Keys of this scene" }).Pressed += _ => ToggleHelp();
        row.AddChild(new Button("Esc Menu") { TooltipText = "Back to the main menu" }).Pressed += _ => BackRequested?.Invoke();

        var info = column.AddChild(new HBoxContainer());
        info.AddThemeConstantOverride("separation", 12f);
        _description = info.AddChild(Text("", Rgba(185, 190, 200), 12f));
        _description.Overrun = TextOverrun.Ellipsis;
        _description.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _description.SizeFlagsStretchRatio = 2f;
        _stats = info.AddChild(Text("", Rgba(130, 135, 145), 12f, HorizontalAlignment.Right));
        _stats.Overrun = TextOverrun.Ellipsis;
        _stats.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return bar;
    }

    private static Engine.CanvasUI.Theme BarTheme(ShowcaseAssets assets)
    {
        var gold = Rgba(255, 196, 40);
        var border = Rgba(90, 100, 120);
        var theme = new Engine.CanvasUI.Theme { DefaultFont = assets.Font, DefaultFontSize = 12f };
        theme.SetStyleBox("Bar", "panel", new StyleBoxFlat(Rgba(7, 9, 14, 220), 10f) { BorderBottom = 1f, BorderColor = border });
        theme.SetStyleBox("Dialog", "panel", new StyleBoxFlat(Rgba(12, 15, 22, 245), 18f).SetBorder(1f, gold));
        theme.SetStyleBox("Button", "normal", Box(Rgba(45, 50, 62), border));
        theme.SetStyleBox("Button", "hover", Box(Rgba(58, 64, 80), Rgba(170, 175, 185)));
        theme.SetStyleBox("Button", "pressed", Box(Rgba(58, 52, 30), gold));
        theme.SetColor("Button", "font_color", Rgba(240, 240, 240));
        theme.SetStyleBox("TooltipPanel", "panel", new StyleBoxFlat(Rgba(10, 13, 19, 240), 6f).SetBorder(1f, gold));
        return theme;
    }

    private static StyleBoxFlat Box(Color4 background, Color4 border)
    {
        var box = new StyleBoxFlat(background).SetBorder(1f, border);
        box.SetContentMargins(8f, 10f);
        return box;
    }
}
