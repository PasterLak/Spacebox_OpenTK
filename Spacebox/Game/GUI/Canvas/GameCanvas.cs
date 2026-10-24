using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI.Canvas;

public sealed class GameCanvas : IDisposable
{
    public static readonly Vector2 VirtualSize = new Vector2(1280f, 720f);
    public static readonly Vector2 MinimumViewport = new Vector2(1024f, 576f);
    public const float MinScale = 0.75f;

    public static void Configure(StretchSettings stretch)
    {
        stretch.Mode = StretchMode.Expand;
        stretch.VirtualSize = VirtualSize;
        stretch.MinimumViewportSize = MinimumViewport;
        stretch.MinScale = MinScale;
    }

    private readonly CanvasLayer _layer = new CanvasLayer("Game interface");
    private readonly GameUi _ui;
    private readonly HudView _hud;
    private readonly Windows.GameWindowsView _windows;
    private readonly Screens.GameScreensView _screens;
    private readonly Sounds.ButtonHoverSounds _hoverSounds = new Sounds.ButtonHoverSounds();
    private readonly InterfaceFontSlot _fonts = new InterfaceFontSlot();
    private GameUiDesign _design = (GameUiDesign)(-1);
    private float _scale = -1f;

    public static GameCanvas Current { get; private set; }

    public static bool DrawsHotbar => true;

    public static bool WantsKeyboard => Current != null && Current._layer.Root.WantsKeyboard;

    public static bool HadOpenDialog => Current != null && Current._dialogWasOpen;

    private bool _dialogWasOpen;

    public static bool IsPointerOverInteractive
    {
        get
        {
            if (Current == null) return false;
            var root = Current._layer.Root;
            for (var control = root.HitTest(root.PointerPosition); control != null; control = control.Parent as Control)
            {
                if (control.IsInteractive) return true;
            }
            return false;
        }
    }

    public CanvasRoot Root => _layer.Root;
    public bool WantsPointer => _layer.Root.WantsPointer;

    public GameCanvas(GameUi ui)
    {
        _ui = ui;
        _hud = new HudView(ui);
        _windows = new Windows.GameWindowsView(ui);
        _screens = new Screens.GameScreensView(ui);
        Current = this;
        var root = _layer.Root;
        Configure(root.Stretch);
        root.Add(_hud, UiLayer.Hud);
        _hud.SetAnchorsPreset(LayoutPreset.FullRect);
        root.Add(_windows, UiLayer.Windows);
        root.Add(_screens, UiLayer.Popup);
        root.Add(new Items.SlotDragGhost(), UiLayer.Popup);
        Apply(Settings.Interface);
    }

    public void Apply(InterfaceSettings settings)
    {
        var design = GameUiDesigns.Parse(settings.Design);
        bool fontChanged = _fonts.Update(settings, out var previousFont);
        if (design != _design || fontChanged)
        {
            _design = design;
            _layer.Root.Theme = GameCanvasThemes.Create(design, _fonts.Font);
        }
        previousFont?.Dispose();

        float scale = Math.Clamp(settings.UiScale, InterfaceSettings.MinUiScale, InterfaceSettings.MaxUiScale);
        if (MathF.Abs(scale - _scale) < 0.001f) return;
        _scale = scale;
        _layer.Root.Stretch.UiScale = scale;
    }

    public void Update(LocalAstronaut player, float deltaTime)
    {
        if (!UIManager.IsUIMode) Items.SlotDrag.Cancel();
        bool interfaceShown = (Settings.ShowInterface || HudVisibility.ShowHotbar) && !_ui.Loading.IsActive;
        if (_hud.Visible != interfaceShown) _hud.Visible = interfaceShown;
        if (_windows.Visible != interfaceShown) _windows.Visible = interfaceShown;
        if (interfaceShown)
        {
            _hud.Refresh(player);
            _windows.Sync();
        }
        _screens.Sync(player?.DeathScreen, deltaTime);
        _layer.InputEnabled = UIManager.IsUIMode || _screens.WantsInput;
        _dialogWasOpen = _layer.Root.TryGetLayer(UiLayer.Modal, out var modal) && modal.HasVisibleContent;
        _layer.Update(deltaTime);
        _hoverSounds.Update(_layer.Root, UIManager.IsUIMode && _layer.InputEnabled);
    }

    public void Render()
    {
        _layer.Render();
    }

    public void Dispose()
    {
        if (Current == this) Current = null;
        _layer.Dispose();
        _fonts.Dispose();
    }
}
