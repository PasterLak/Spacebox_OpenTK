using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class SplitContainer : Container
{
    private float _ratio = 0.3f;
    private bool _dragging;

    public SplitContainer(bool vertical = false)
    {
        Vertical = vertical;
        MouseFilter = MouseFilter.Pass;
    }

    public bool Vertical { get; }
    public float HandleSize { get; set; } = 6f;
    public float MinFirst { get; set; } = 80f;
    public float MinSecond { get; set; } = 80f;

    public event Action<float> RatioChanged;

    public float Ratio
    {
        get => _ratio;
        set
        {
            float clamped = Math.Clamp(value, 0.02f, 0.98f);
            if (_ratio == clamped) return;
            _ratio = clamped;
            QueueLayout();
        }
    }

    public override Vector2 GetMinimumSize()
    {
        var (first, second) = Panes();
        var a = first?.GetCombinedMinimumSize() ?? Vector2.Zero;
        var b = second?.GetCombinedMinimumSize() ?? Vector2.Zero;
        return Vertical ? new Vector2(MathF.Max(a.X, b.X), a.Y + b.Y + HandleSize) : new Vector2(a.X + b.X + HandleSize, MathF.Max(a.Y, b.Y));
    }

    protected override void LayoutChildren()
    {
        var (first, second) = Panes();
        float total = Vertical ? Rect.Height : Rect.Width;
        float split = SplitPosition(total);
        if (first != null)
            FitChildInRect(first, Vertical ? new Rect2(Rect.X, Rect.Y, Rect.Width, split) : new Rect2(Rect.X, Rect.Y, split, Rect.Height));
        if (second != null)
        {
            float start = split + HandleSize;
            FitChildInRect(second, Vertical ? new Rect2(Rect.X, Rect.Y + start, Rect.Width, MathF.Max(0f, total - start)) : new Rect2(Rect.X + start, Rect.Y, MathF.Max(0f, total - start), Rect.Height));
        }
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left || !OnHandle(localPosition)) return false;
        _dragging = true;
        return true;
    }

    protected internal override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        if (!_dragging || !pressed) return;
        float total = Vertical ? Size.Y : Size.X;
        if (total <= 0f) return;
        float position = (Vertical ? localPosition.Y : localPosition.X) - HandleSize * 0.5f;
        Ratio = position / total;
        RatioChanged?.Invoke(_ratio);
    }

    protected internal override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside)
    {
        if (!_dragging) return false;
        _dragging = false;
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        float total = Vertical ? Size.Y : Size.X;
        float split = SplitPosition(total);
        var handle = Vertical ? new Rect2(0f, split, Size.X, HandleSize) : new Rect2(split, 0f, HandleSize, Size.Y);
        var color = _dragging || IsHovered && OnHandle(Root.PointerPosition - GlobalPosition)
            ? GetThemeColor("handle_hover_color", new Color4(1f, 0.77f, 0.16f, 0.6f))
            : GetThemeColor("handle_color", new Color4(1f, 1f, 1f, 0.08f));
        float thickness = 2f;
        var line = Vertical ? new Rect2(handle.X, handle.Y + (HandleSize - thickness) * 0.5f, handle.Width, thickness) : new Rect2(handle.X + (HandleSize - thickness) * 0.5f, handle.Y, thickness, handle.Height);
        renderer.DrawRect(line, color);
    }

    private bool OnHandle(Vector2 localPosition)
    {
        float total = Vertical ? Size.Y : Size.X;
        float split = SplitPosition(total);
        float value = Vertical ? localPosition.Y : localPosition.X;
        return value >= split - 2f && value <= split + HandleSize + 2f;
    }

    private float SplitPosition(float total)
    {
        float available = MathF.Max(0f, total - HandleSize);
        float min = MathF.Min(MinFirst, available * 0.5f);
        float max = MathF.Max(min, available - MathF.Min(MinSecond, available * 0.5f));
        return MathF.Floor(Math.Clamp(available * _ratio, min, max));
    }

    private (Control First, Control Second) Panes()
    {
        Control first = null, second = null;
        foreach (var child in Children)
        {
            if (child is not Control control || !control.Visible) continue;
            if (first == null) first = control;
            else if (second == null) second = control;
        }
        return (first, second);
    }
}
