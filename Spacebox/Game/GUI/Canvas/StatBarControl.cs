using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas;

public sealed class StatBarControl : Control
{
    private float _ratio;
    private string _text = "";
    private int _shownValue = int.MinValue;
    private int _shownMax = int.MinValue;
    private string _shownOverride;

    public StatBarControl(Color4 fill)
    {
        Fill = fill;
        MouseFilter = MouseFilter.Ignore;
    }

    public Color4 Fill { get; set; }

    public void Set(int value, int max, string overrideText)
    {
        if (value == _shownValue && max == _shownMax && overrideText == _shownOverride) return;
        _shownValue = value;
        _shownMax = max;
        _shownOverride = overrideText;
        _ratio = max > 0 ? Math.Clamp(value / (float)max, 0f, 1f) : 0f;
        _text = string.IsNullOrEmpty(overrideText) ? $"{value}/{max}" : overrideText;
    }

    public override Vector2 GetMinimumSize()
    {
        var font = GetThemeFont();
        float height = font != null ? font.GetLineHeight(GetThemeFontSize()) + 4f : 16f;
        return Vector2.ComponentMax(base.GetMinimumSize(), new Vector2(60f, height));
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var rect = LocalRect;
        renderer.DrawRect(rect, GetThemeColor("bar_back_color", new Color4(0.2f, 0.2f, 0.2f, 0.85f)));
        if (_ratio > 0f) renderer.DrawRect(new Rect2(rect.X, rect.Y, MathF.Round(rect.Width * _ratio), rect.Height), Fill);

        var font = GetThemeFont();
        if (font == null || _text.Length == 0) return;
        float size = GetThemeFontSize();
        var textSize = font.Measure(_text, size);
        var position = new Vector2(MathF.Round((rect.Width - textSize.X) * 0.5f), MathF.Round((rect.Height - textSize.Y) * 0.5f));
        CanvasDraw.Text(renderer, font, position + Vector2.One, _text, size, GetThemeColor("font_shadow_color", Color4.Black));
        CanvasDraw.Text(renderer, font, position, _text, size, GetThemeColor("hud_text_color", Color4.White));
    }
}
