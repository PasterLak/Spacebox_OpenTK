using Engine.CanvasUI;
using Engine.GUI;

namespace Engine.UI;

public sealed class DevPanel : IDisposable
{
    private const float WidthRatio = 0.26f;
    private const float RefreshInterval = 0.5f;

    private readonly DebugCanvas _canvas = new DebugCanvas("Developer panel");
    private readonly List<DevPanelSection> _sections = new List<DevPanelSection>();
    private float _refreshTimer;

    public static DevPanel Instance { get; private set; }

    public bool WantsPointer => _canvas.Root.WantsPointer;

    public static void Update()
    {
        if (Instance != null) Instance._canvas.Layer.Visible = false;
        if (!Overlay.IsVisible || Application.IsHeadless) return;

        Instance ??= new DevPanel();
        Instance.Tick(Time.Delta);
    }

    public static void Draw()
    {
        Instance?._canvas.Layer.Render();
    }

    public static void Shutdown()
    {
        Instance?.Dispose();
        Instance = null;
    }

    private DevPanel()
    {
        var root = _canvas.Root;
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

        _canvas.Layer.Visible = true;
        _canvas.Layer.Update(deltaTime);
    }

    private void RefreshAll()
    {
        foreach (var section in _sections) section.Refresh();
    }

    public void Dispose()
    {
        _canvas.Dispose();
    }
}
