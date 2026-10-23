using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class TankGauge : Control
{
    private const float BarWidth = 22f;
    private const float TickLength = 5f;
    private static readonly Color4 Background = new Color4(0.1f, 0.1f, 0.12f, 1f);
    private static readonly Color4 Border = new Color4(0.7f, 0.7f, 0.7f, 1f);

    private readonly string[] _labels;
    private readonly int _steps;
    private int _capacity = -1;

    public TankGauge(int steps, float height, Color4 fill)
    {
        _steps = Math.Max(1, steps);
        _labels = new string[_steps + 1];
        Fill = fill;
        MouseFilter = MouseFilter.Ignore;
        CustomMinimumSize = new Vector2(BarWidth + 40f, height);
    }

    public Color4 Fill { get; set; }
    public int Value { get; set; }

    public int Capacity
    {
        get => _capacity;
        set
        {
            if (value == _capacity) return;
            _capacity = value;
            for (int i = 0; i <= _steps; i++) _labels[i] = ((int)MathF.Round(value * i / (float)_steps)).ToString();
        }
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var bar = new Rect2(0f, 0f, BarWidth, Size.Y);
        float ratio = _capacity > 0 ? Math.Clamp(Value / (float)_capacity, 0f, 1f) : 0f;
        renderer.DrawRect(bar, Background);
        float filled = MathF.Round(bar.Height * ratio);
        renderer.DrawRect(new Rect2(bar.X, bar.Y + bar.Height - filled, bar.Width, filled), Fill);
        CanvasDraw.Outline(renderer, bar, Border, 1f, 1f, 1f, 1f);

        var font = GetThemeFont();
        if (font == null || _capacity < 0) return;
        float size = GetThemeFontSize();
        float lineHeight = font.GetLineHeight(size);
        var color = GetThemeColor("font_color", Color4.White);
        for (int i = 0; i <= _steps; i++)
        {
            float y = MathF.Round(bar.Height - bar.Height * i / _steps);
            renderer.DrawRect(new Rect2(bar.Width, Math.Min(y, bar.Height - 1f), TickLength, 1f), Border);
            float labelY = Math.Clamp(y - lineHeight * 0.5f, 0f, bar.Height - lineHeight);
            CanvasDraw.Text(renderer, font, new Vector2(bar.Width + TickLength + 3f, MathF.Round(labelY)), _labels[i], size, color);
        }
    }
}
