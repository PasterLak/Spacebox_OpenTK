using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.MenuUi;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class PauseScreen : GameScreen
{
    private const float MenuWidth = 230f;
    private const float TitleSize = 72f;

    private static readonly Color4 DimColor = new Color4(0f, 0f, 0f, 0.7f);
    private static readonly Color4 TitleColor = new Color4(250 / 255f, 186 / 255f, 0f, 1f);
    private static readonly Color4 NearShadow = new Color4(86 / 255f, 69 / 255f, 17 / 255f, 0.8f);
    private static readonly Color4 FarShadow = new Color4(40 / 255f, 30 / 255f, 5 / 255f, 0.8f);

    private readonly ParallaxTitle _title = new ParallaxTitle("PAUSE", TitleSize, TitleColor, NearShadow, FarShadow, 4f);
    private readonly CenterContainer _center = new CenterContainer { MouseFilter = MouseFilter.Ignore };
    private readonly StatisticsPanel _statistics;
    private readonly PlayerListPanel _players;
    private readonly HBoxContainer _leftSide = new HBoxContainer { MouseFilter = MouseFilter.Ignore, Alignment = BoxAlignment.End };
    private readonly HBoxContainer _rightSide = new HBoxContainer { MouseFilter = MouseFilter.Ignore };
    private readonly Button _save;
    private readonly Button _network;
    private OptionsHostPage _options;
    private Spacebox.Game.GUI.Menu.OptionsMenu _optionsMenu;

    private readonly PauseUI _pause;

    public PauseScreen(PauseUI pause, StatisticsUI statistics)
    {
        _pause = pause;
        _statistics = new StatisticsPanel(pause, statistics);
        _players = new PlayerListPanel(pause);
        Name = "Pause";
        AddChild(_center);
        _center.SetAnchorsPreset(LayoutPreset.FullRect);

        var column = _center.AddChild(new VBoxContainer { MouseFilter = MouseFilter.Ignore });
        column.AddThemeConstantOverride("separation", 12f);
        column.AddChild(_title);
        var row = column.AddChild(new HBoxContainer { MouseFilter = MouseFilter.Ignore });
        row.AddThemeConstantOverride("separation", 16f);
        row.AddChild(_leftSide);
        _leftSide.AddChild(_players);

        var frame = MenuLayout.Window(null, MenuWidth);
        frame.Root.Name = "PauseMenu";
        frame.Footer.Visible = false;
        frame.Body.AddThemeConstantOverride("separation", 10f);
        row.AddChild(frame.Root);

        frame.Body.AddChild(MenuLayout.Button(null, "Continue", _pause.Continue));
        _save = frame.Body.AddChild(MenuLayout.Button(null, "Save", _pause.Save));
        frame.Body.AddChild(MenuLayout.Button(null, "Options", _pause.OpenOptions));
        frame.Body.AddChild(MenuLayout.Button(null, "Statistics", _pause.ToggleStatistics));
        frame.Body.AddChild(MenuLayout.Button(null, "Tutorial", _pause.OpenTutorial));
        _network = frame.Body.AddChild(MenuLayout.Button(null, "Open to network", _pause.OpenToNetwork));
        frame.Body.AddChild(MenuLayout.Button(null, "Go to menu", _pause.RequestGoToMenuFromButton));
        frame.Body.AddChild(MenuLayout.Button(null, "Exit", _pause.RequestExit));

        row.AddChild(_rightSide);
        _rightSide.AddChild(_statistics);
    }

    public StatisticsPanel Statistics => _statistics;

    public PlayerListPanel Players => _players;

    public override bool IsShown => _pause.IsVisible;

    protected override bool BlocksPointer => true;

    protected override void Refresh(float deltaTime)
    {
        bool options = _pause.IsOptionsOpen && SyncOptions();
        bool menu = !options && !_pause.IsTutorialOpen;

        if (_center.Visible != menu) _center.Visible = menu;
        if (_options != null && _options.Visible != options)
        {
            _options.Visible = options;
            if (options) _options.OnShow();
        }

        if (options) _options.Tick(deltaTime);
        if (!menu) return;

        _title.Intensity = _pause.ParallaxIntensity;
        _title.Tick();
        if (_save.Text != _pause.SaveButtonText) _save.Text = _pause.SaveButtonText;
        if (_network.Text != _pause.NetworkButtonText) _network.Text = _pause.NetworkButtonText;
        _statistics.Sync(deltaTime);
        _players.Sync(deltaTime);
        BalanceSides();
    }

    private void BalanceSides()
    {
        float left = _players.Visible ? _players.GetCombinedMinimumSize().X : 0f;
        float right = _statistics.Visible ? _statistics.GetCombinedMinimumSize().X : 0f;
        float side = MathF.Max(left, right);
        if (_leftSide.CustomMinimumSize.X != side) _leftSide.CustomMinimumSize = new Vector2(side, 0f);
        if (_rightSide.CustomMinimumSize.X != side) _rightSide.CustomMinimumSize = new Vector2(side, 0f);
    }

    private bool SyncOptions()
    {
        var menu = _pause.Options;
        if (menu == null) return false;
        if (ReferenceEquals(menu, _optionsMenu)) return true;

        if (_options != null) RemoveChild(_options);
        _optionsMenu = menu;
        _options = new OptionsHostPage(menu) { Visible = false };
        _options.SetAnchorsPreset(LayoutPreset.FullRect);
        _options.EnsureBuilt();
        AddChild(_options);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, DimColor);
    }
}
