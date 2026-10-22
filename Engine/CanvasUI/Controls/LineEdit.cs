using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class LineEdit : TextEditBase
{
    private float _scroll;

    public LineEdit() : base(false)
    {
    }

    public LineEdit(string text) : this()
    {
        Text = text;
    }

    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    public override Vector2 GetMinimumSize()
    {
        var font = GetThemeFont();
        float height = font?.GetLineHeight(GetThemeFontSize()) ?? 12f;
        var style = GetThemeStyleBox("normal");
        return new Vector2(24f, height) + (style?.MinimumSize ?? Vector2.Zero);
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = CurrentStyle();
        style?.Draw(renderer, LocalRect);
        var inner = style == null ? LocalRect : style.ContentRect(LocalRect);
        var font = GetThemeFont();
        if (font == null) return;

        float size = GetThemeFontSize();
        float lineHeight = font.GetLineHeight(size);
        float y = MathF.Floor(inner.Y + (inner.Height - lineHeight) * 0.5f);
        float origin = TextOrigin(font, size, inner);

        renderer.PushClip(inner);
        string text = State.Text;
        if (text.Length == 0 && !HasFocus && PlaceholderText.Length > 0)
        {
            CanvasDraw.Text(renderer, font, new Vector2(origin, y), PlaceholderText, size, GetThemeColor("placeholder_color", new Color4(1f, 1f, 1f, 0.35f)));
        }
        else
        {
            if (State.HasSelection && HasFocus)
            {
                float x0 = origin + TextMetrics.XOf(font, text, State.SelectionStart, size);
                float x1 = origin + TextMetrics.XOf(font, text, State.SelectionEnd, size);
                DrawSelection(renderer, new Rect2(x0, y, x1 - x0, lineHeight));
            }

            var color = Editable ? GetThemeColor("font_color", Color4.White) : GetThemeColor("font_readonly_color", GetThemeColor("font_color", Color4.White));
            CanvasDraw.Text(renderer, font, new Vector2(origin, y), text, size, color);
            DrawCaret(renderer, new Vector2(origin + TextMetrics.XOf(font, text, State.Caret, size), y), lineHeight);
        }
        renderer.PopClip();
    }

    protected override int IndexAtPoint(Vector2 localPosition)
    {
        var font = GetThemeFont();
        if (font == null) return 0;
        var inner = InnerRect();
        float size = GetThemeFontSize();
        return TextMetrics.IndexAt(font, State.Text, localPosition.X - TextOrigin(font, size, inner), size);
    }

    protected override void EnsureCaretVisible()
    {
        var font = GetThemeFont();
        if (font == null) return;

        float size = GetThemeFontSize();
        float width = InnerRect().Width;
        float caret = TextMetrics.XOf(font, State.Text, State.Caret, size);
        if (caret - _scroll > width - 2f) _scroll = caret - width + 2f;
        if (caret - _scroll < 0f) _scroll = caret;
        float total = font.MeasureWidth(State.Text, size);
        _scroll = Math.Clamp(_scroll, 0f, MathF.Max(0f, total - width + 2f));
    }

    protected override void OnTextReplaced()
    {
        _scroll = 0f;
    }

    private Rect2 InnerRect()
    {
        var style = GetThemeStyleBox("normal");
        return style == null ? LocalRect : style.ContentRect(LocalRect);
    }

    private float TextOrigin(CanvasFont font, float size, Rect2 inner)
    {
        float x = inner.X - _scroll;
        if (Alignment == HorizontalAlignment.Left || HasFocus) return MathF.Floor(x);

        float width = font.MeasureWidth(State.Text, size);
        if (width >= inner.Width) return MathF.Floor(x);
        return MathF.Floor(Alignment == HorizontalAlignment.Center ? inner.X + (inner.Width - width) * 0.5f : inner.End.X - width);
    }
}
