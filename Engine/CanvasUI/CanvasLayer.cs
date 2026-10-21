using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public sealed class CanvasLayer : IDisposable
{
    private readonly GlCanvasRenderer _renderer = new GlCanvasRenderer();
    private readonly Label _tooltipLabel = new Label();
    private readonly PanelContainer _tooltip = new PanelContainer { MouseFilter = MouseFilter.Ignore, ThemeTypeVariation = "TooltipPanel", Visible = false };

    public CanvasRoot Root { get; } = new CanvasRoot();
    public GlCanvasRenderer Renderer => _renderer;
    public bool InputEnabled { get; set; } = true;

    public CanvasLayer()
    {
        _tooltip.AddChild(_tooltipLabel);
        _tooltipLabel.ThemeTypeVariation = "TooltipLabel";
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
        UpdateTooltip();
    }

    public void Render()
    {
        var window = EngineWindow.Instance;
        if (window == null || Application.IsHeadless) return;

        var screen = new Vector2(window.ClientSize.X, window.ClientSize.Y);
        Root.Resize(screen);
        _renderer.Begin(screen, Root.ScreenTransform);
        Root.Draw(_renderer);
        _renderer.End();
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }

    private void UpdateTooltip()
    {
        string text = Root.TooltipText;
        if (text == null)
        {
            _tooltip.Visible = false;
            return;
        }

        if (_tooltip.Root != Root)
        {
            Root.Content.AddChild(_tooltip);
            _tooltip.Visible = false;
        }

        _tooltipLabel.Text = text;
        _tooltip.Visible = true;
        var size = _tooltip.GetCombinedMinimumSize();
        var position = Root.PointerPosition + new Vector2(14f, 18f);
        position.X = MathF.Min(position.X, Root.ViewportSize.X - size.X - 2f);
        position.Y = MathF.Min(position.Y, Root.ViewportSize.Y - size.Y - 2f);
        position = new Vector2(MathF.Floor(position.X), MathF.Floor(position.Y));
        if (_tooltip.Position != position || _tooltip.RequestedSize != size)
            _tooltip.SetOffsets(position.X, position.Y, position.X + size.X, position.Y + size.Y);
        if (Root.Content.Children[^1] != _tooltip) _tooltip.MoveToFront();
    }
}
