using Engine.CanvasUI.Validation;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public sealed class CanvasLayer : IDisposable
{
    private const float ValidationInterval = 0.5f;

    private static readonly List<CanvasLayer> _active = new List<CanvasLayer>();

    private readonly GlCanvasRenderer _renderer = new GlCanvasRenderer();
    private LayoutReport _report;
    private float _validationTimer;

    public static IReadOnlyList<CanvasLayer> Active => _active;

    public string Name { get; set; }
    public CanvasRoot Root { get; } = new CanvasRoot();
    public GlCanvasRenderer Renderer => _renderer;
    public bool InputEnabled { get; set; } = true;
    public bool ShowLayoutIssues { get; set; }
    public LayoutRules ValidationRules { get; set; } = new LayoutRules();
    public LayoutReport LastReport => _report;

    public CanvasLayer(string name = "Canvas")
    {
        Name = name;
        _active.Add(this);
    }

    public LayoutReport Validate()
    {
        _report = LayoutValidator.Validate(Root, ValidationRules, $"{Root.ScreenSize.X:0}x{Root.ScreenSize.Y:0}");
        return _report;
    }

    public void Update(float deltaTime)
    {
        var window = EngineWindow.Instance;
        if (window == null) return;

        Root.Resize(new Vector2(window.ClientSize.X, window.ClientSize.Y));
        var pointer = InputEnabled
            ? new PointerState
            {
                Position = Input.Mouse.Position,
                LeftDown = Input.Mouse.IsButtonDown(MouseButton.Left),
                RightDown = Input.Mouse.IsButtonDown(MouseButton.Right),
                Scroll = Input.Mouse.ScrollDelta.Y
            }
            : default;
        Root.Update(deltaTime, pointer);

        if (!ShowLayoutIssues) return;
        _validationTimer -= deltaTime;
        if (_validationTimer > 0f) return;
        _validationTimer = ValidationInterval;
        Validate();
    }

    public void Render()
    {
        var window = EngineWindow.Instance;
        if (window == null || Application.IsHeadless) return;

        var screen = new Vector2(window.ClientSize.X, window.ClientSize.Y);
        Root.Resize(screen);
        _renderer.Begin(screen, Root.ScreenTransform);
        Root.Draw(_renderer);
        if (ShowLayoutIssues) LayoutDebugOverlay.Draw(_renderer, Root, _report);
        _renderer.End();
    }

    public void Dispose()
    {
        _active.Remove(this);
        _renderer.Dispose();
    }
}
