using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI.Canvas;

public sealed class GameCanvas : IDisposable
{
    public static readonly Vector2 VirtualSize = new Vector2(1280f, 720f);

    private readonly CanvasLayer _layer = new CanvasLayer("Game interface");
    private readonly HudView _hud = new HudView();
    private SkiaCanvasFont _font;
    private GameUiDesign _design = (GameUiDesign)(-1);
    private float _scale = -1f;

    public static GameCanvas Current { get; private set; }

    public static bool DrawsHud => Current != null;

    public CanvasRoot Root => _layer.Root;
    public bool WantsPointer => _layer.Root.WantsPointer;

    public GameCanvas()
    {
        Current = this;
        var root = _layer.Root;
        root.Stretch.Mode = StretchMode.Expand;
        root.Stretch.VirtualSize = VirtualSize;
        root.Add(_hud, UiLayer.Hud);
        _hud.SetAnchorsPreset(LayoutPreset.FullRect);
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
        _layer.InputEnabled = UIManager.IsUIMode;
        _hud.Refresh(player);
        _layer.Update(deltaTime);
    }

    public void Render()
    {
        if (!Settings.ShowInterface && !HudVisibility.ShowHotbar) return;
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
