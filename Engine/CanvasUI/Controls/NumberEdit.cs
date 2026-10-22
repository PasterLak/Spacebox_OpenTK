using System.Globalization;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class NumberEdit : LineEdit
{
    private const float ButtonWidth = 14f;

    private double _value;
    private bool _integer;

    public NumberEdit()
    {
        Alignment = HorizontalAlignment.Right;
        SelectAllOnFocus = true;
        EditingFinished += _ => Commit();
        TextSubmitted += _ => Commit();
        Text = Format(_value);
    }

    public double MinValue { get; set; } = double.MinValue;
    public double MaxValue { get; set; } = double.MaxValue;
    public double Step { get; set; } = 1;
    public int Decimals { get; set; } = 3;

    public event Action<double> ValueChanged;

    public bool Integer
    {
        get => _integer;
        set
        {
            _integer = value;
            Text = Format(_value);
        }
    }

    public double Value
    {
        get => _value;
        set => SetValue(value, false);
    }

    public static bool TryParse(string text, out double value)
    {
        text = (text ?? "").Trim().Replace(',', '.');
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }

    public string Format(double value)
    {
        if (_integer) return Math.Round(value).ToString("0", CultureInfo.InvariantCulture);
        return Math.Round(value, Math.Clamp(Decimals, 0, 10)).ToString("0.##########", CultureInfo.InvariantCulture);
    }

    public override Vector2 GetMinimumSize() => base.GetMinimumSize() + new Vector2(ButtonWidth, 0f);

    protected internal override bool OnKeyPress(KeyPress press)
    {
        if (HasFocus && press.Key == Keys.Up) { Nudge(1); return true; }
        if (HasFocus && press.Key == Keys.Down) { Nudge(-1); return true; }
        return base.OnKeyPress(press);
    }

    protected internal override bool OnScroll(Vector2 localPosition, float delta)
    {
        if (!HasFocus || !Editable) return false;
        Nudge(delta > 0f ? 1 : -1);
        return true;
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button == MouseButton.Left && Editable && localPosition.X >= Size.X - ButtonWidth)
        {
            Nudge(localPosition.Y < Size.Y * 0.5f ? 1 : -1);
            return true;
        }
        return base.OnMouseDown(localPosition, button);
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        base.Draw(renderer);
        if (!Editable) return;

        var color = GetThemeColor(IsHovered ? "font_color" : "placeholder_color", new Color4(1f, 1f, 1f, 0.4f));
        float x = Size.X - ButtonWidth + 4f;
        float mid = Size.Y * 0.5f;
        renderer.DrawRect(new Rect2(x + 2f, mid - 5f, 2f, 2f), color);
        renderer.DrawRect(new Rect2(x, mid - 3f, 6f, 2f), color);
        renderer.DrawRect(new Rect2(x, mid + 1f, 6f, 2f), color);
        renderer.DrawRect(new Rect2(x + 2f, mid + 3f, 2f, 2f), color);
    }

    private void Nudge(int direction)
    {
        double step = Step <= 0 ? 1 : Step;
        SetValue((TryParse(Text, out var current) ? current : _value) + direction * step, true);
        SelectAll();
    }

    private void Commit()
    {
        if (TryParse(Text, out var parsed)) SetValue(parsed, true);
        else Text = Format(_value);
    }

    private void SetValue(double value, bool notify)
    {
        value = Math.Clamp(value, MinValue, MaxValue);
        if (_integer) value = Math.Round(value);
        bool changed = value != _value;
        _value = value;
        string text = Format(value);
        if (Text != text) Text = text;
        if (changed && notify) ValueChanged?.Invoke(value);
    }
}
