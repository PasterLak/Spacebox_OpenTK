using Engine.CanvasUI;
using Engine.GUI;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class DevPanel : IDisposable
{
    private const float WidthRatio = 0.26f;
    private const float RefreshInterval = 0.5f;

    private readonly CanvasLayer _layer = new CanvasLayer("Developer panel");
    private readonly List<DevPanelSection> _sections = new List<DevPanelSection>();
    private SkiaCanvasFont _font;
    private float _refreshTimer;

    public static DevPanel Instance { get; private set; }

    public bool WantsPointer => _layer.Root.WantsPointer;

    public static void Draw()
    {
        if (!Overlay.IsVisible || Application.IsHeadless) return;

        Instance ??= new DevPanel();
        Instance.Tick(Time.Delta);
    }

    public static void Shutdown()
    {
        Instance?.Dispose();
        Instance = null;
    }

    private DevPanel()
    {
        string fontPath = Path.Combine(AppContext.BaseDirectory, DarkTheme.FontPath);
        _font = new SkiaCanvasFont(fontPath, (int)DarkTheme.FontSize);
        var root = _layer.Root;
        root.Theme = DarkTheme.Create(_font);
        root.Stretch.Mode = StretchMode.Expand;
        root.Stretch.VirtualSize = new Vector2(1280f, 720f);
        root.Stretch.ScaleStep = 0.5f;
        root.Stretch.MinScale = 1f;

        var panel = new PanelContainer { Name = "DevPanel" };
        root.Add(panel, UiLayer.Debug);
        panel.AnchorLeft = 1f - WidthRatio;
        panel.AnchorRight = 1f;
        panel.AnchorTop = 0f;
        panel.AnchorBottom = 1f;

        var scroll = new ScrollContainer();
        panel.AddChild(scroll);
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        column.AddThemeConstantOverride("separation", 8f);
        scroll.AddChild(column);

        _sections.Add(new DevSceneSection());
        _sections.Add(new DevLightingSection());
        var inspector = new DevNodeInspectorSection();
        _sections.Add(new DevHierarchySection(inspector.Select));
        _sections.Add(inspector);
        foreach (var section in _sections) column.AddChild(section);
        RefreshAll();
    }

    private void Tick(float deltaTime)
    {
        _refreshTimer -= deltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = RefreshInterval;
            RefreshAll();
        }

        _layer.Update(deltaTime);
        _layer.Render();
    }

    private void RefreshAll()
    {
        foreach (var section in _sections) section.Refresh();
    }

    public void Dispose()
    {
        _layer.Dispose();
        _font?.Dispose();
        _font = null;
    }
}
