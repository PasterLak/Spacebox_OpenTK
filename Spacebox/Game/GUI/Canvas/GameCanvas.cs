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
    private SkiaCanvasFont _font;
    private GameUiDesign _design = (GameUiDesign)(-1);
    private float _scale = -1f;

    public static GameCanvas Current { get; private set; }

    public static bool DrawsHotbar => true;

    public static bool WantsKeyboard => Current != null && Current._layer.Root.WantsKeyboard;

    public CanvasRoot Root => _layer.Root;
    public bool WantsPointer => _layer.Root.WantsPointer;

    public GameCanvas()
    {
        Current = this;
        Items.SlotDrag.IsPointerOverOtherUi = () => ImGuiNET.ImGui.GetIO().WantCaptureMouse;
        var root = _layer.Root;
        Configure(root.Stretch);
        root.Add(_hud, UiLayer.Hud);
        _hud.SetAnchorsPreset(LayoutPreset.FullRect);
        root.Add(_windows, UiLayer.Windows);
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
        _layer.InputEnabled = UIManager.IsUIMode;
        Items.SlotDrag.BeginFrame();
        if (!UIManager.IsUIMode) Items.SlotDrag.Cancel();
        HitMarkerUI.Update(deltaTime);
        _hud.Refresh(player);
        _windows.Sync();
        _layer.Update(deltaTime);
        PublishInventoryRect();
    }

    public bool TryAcceptImGuiDrop(ItemSlot source)
    {
        var root = _layer.Root;
        var hovered = root.HitTest(root.PointerPosition);
        if (hovered == null) return false;

        Items.SlotDrag.FindTarget(hovered)?.AcceptDrop(source);
        return true;
    }

    private void PublishInventoryRect()
    {
        var window = _windows.Inventory;
        if (!window.Visible)
        {
            InventoryUI.SetWindowRect(System.Numerics.Vector2.Zero, System.Numerics.Vector2.Zero);
            return;
        }

        var root = _layer.Root;
        var min = root.CanvasToScreen(window.Rect.Position);
        var max = root.CanvasToScreen(window.Rect.Position + window.Rect.Size);
        InventoryUI.SetWindowRect(new System.Numerics.Vector2(min.X, min.Y), new System.Numerics.Vector2(max.X - min.X, max.Y - min.Y));
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
