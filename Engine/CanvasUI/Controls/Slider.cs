using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class HSlider : Control
{
    private double _value;

    public HSlider()
    {
        FocusMode = FocusMode.Click;
    }

    public double MinValue { get; set; }
    public double MaxValue { get; set; } = 1;
    public double Step { get; set; }
    public override bool IsInteractive => true;
    public Color4? FillColor { get; set; }

    public event Action<double> ValueChanged;

    public double Value
    {
        get => _value;
        set => SetValue(value, false);
    }

    public double Ratio => MaxValue > MinValue ? (_value - MinValue) / (MaxValue - MinValue) : 0;

    public override Vector2 GetMinimumSize() => new Vector2(60f, 16f);

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        SetFromX(localPosition.X);
        return true;
    }

    protected internal override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        if (pressed && IsMouseDown) SetFromX(localPosition.X);
    }

    protected internal override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside) => true;

    protected internal override bool OnScroll(Vector2 localPosition, float delta)
    {
        if (!HasFocus) return false;
        double step = Step > 0 ? Step : (MaxValue - MinValue) / 100.0;
        SetValue(_value + Math.Sign(delta) * step, true);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        float mid = MathF.Floor(Size.Y * 0.5f);
        renderer.DrawRect(new Rect2(0f, mid - 2f, Size.X, 4f), GetThemeColor("track_color", new Color4(1f, 1f, 1f, 0.12f)));
        float x = (float)(Ratio * Size.X);
        renderer.DrawRect(new Rect2(0f, mid - 2f, x, 4f), FillColor ?? GetThemeColor("fill_color", new Color4(1f, 0.77f, 0.16f, 0.8f)));
        renderer.DrawRect(new Rect2(MathF.Floor(x) - 3f, mid - 6f, 6f, 12f), GetThemeColor(IsHovered ? "grabber_hover_color" : "grabber_color", Color4.White));
    }

    private void SetFromX(float x)
    {
        double ratio = Size.X <= 0f ? 0 : Math.Clamp(x / Size.X, 0f, 1f);
        SetValue(MinValue + ratio * (MaxValue - MinValue), true);
    }

    private void SetValue(double value, bool notify)
    {
        if (Step > 0) value = MinValue + Math.Round((value - MinValue) / Step) * Step;
        value = Math.Clamp(value, MinValue, MaxValue);
        if (value == _value) return;
        _value = value;
        if (notify) ValueChanged?.Invoke(value);
    }
}

public class HSeparator : Control
{
    public HSeparator()
    {
        MouseFilter = MouseFilter.Ignore;
    }

    public override Vector2 GetMinimumSize() => new Vector2(0f, GetThemeConstant("separation", 7f));

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(new Rect2(0f, MathF.Floor(Size.Y * 0.5f), Size.X, 1f), GetThemeColor("color", new Color4(1f, 1f, 1f, 0.12f)));
    }
}

public class VSeparator : Control
{
    public VSeparator()
    {
        MouseFilter = MouseFilter.Ignore;
    }

    public override Vector2 GetMinimumSize() => new Vector2(GetThemeConstant("separation", 7f), 0f);

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(new Rect2(MathF.Floor(Size.X * 0.5f), 0f, 1f, Size.Y), GetThemeColor("color", new Color4(1f, 1f, 1f, 0.12f)));
    }
}
