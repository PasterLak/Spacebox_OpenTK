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
    private readonly HudView _hud = new HudView();
    private readonly Windows.GameWindowsView _windows = new Windows.GameWindowsView();
    private readonly Screens.GameScreensView _screens = new Screens.GameScreensView();
    private readonly Sounds.ButtonHoverSounds _hoverSounds = new Sounds.ButtonHoverSounds();
    private SkiaCanvasFont _font;
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

    public GameCanvas()
    {
        Current = this;
        var root = _layer.Root;
        Configure(root.Stretch);
        root.Add(_hud, UiLayer.Hud);
        _hud.SetAnchorsPreset(LayoutPreset.FullRect);
        root.Add(_windows, UiLayer.Windows);
        root.Add(_screens, UiLayer.Popup);
        root.Add(new Items.SlotDragGhost(), UiLayer.Popup);
        if (!Application.IsHeadless) _font = new SkiaCanvasFont(Path.Combine(AppContext.BaseDirectory, GameCanvasThemes.FontPath), (int)GameCanvasThemes.FontSize);
        Apply(Settings.Interface);
    }

    public void Apply(InterfaceSettings settings)
    {
        var design = GameUiDesigns.Parse(settings.Design);
        if (design != _design)
        {
            _design = design;
            _layer.Root.Theme = GameCanvasThemes.Create(design, _font);
        }

        float scale = Math.Clamp(settings.UiScale, InterfaceSettings.MinUiScale, InterfaceSettings.MaxUiScale);
        if (MathF.Abs(scale - _scale) < 0.001f) return;
        _scale = scale;
        _layer.Root.Stretch.UiScale = scale;
    }

    public void Update(LocalAstronaut player, float deltaTime)
    {
        if (!UIManager.IsUIMode) Items.SlotDrag.Cancel();
        HitMarkerUI.Update(deltaTime);
        bool interfaceShown = (Settings.ShowInterface || HudVisibility.ShowHotbar) && !WorldLoadingScreen.IsActive;
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
        _font?.Dispose();
        _font = null;
    }
}
