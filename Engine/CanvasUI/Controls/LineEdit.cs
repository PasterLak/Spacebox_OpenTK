using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class LineEdit : TextEditBase
{
    private float _scroll;
    private string _mask = "";

    public LineEdit() : base(false)
    {
    }

    public LineEdit(string text) : this()
    {
        Text = text;
    }

    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
    public bool Secret { get; set; }
    public char SecretCharacter { get; set; } = '*';

    protected override ICanvasClipboard Clipboard => Secret ? new PasteOnlyClipboard(base.Clipboard) : base.Clipboard;

    private string DisplayText
    {
        get
        {
            if (!Secret) return State.Text;
            int length = State.Text.Length;
            if (_mask.Length != length || (length > 0 && _mask[0] != SecretCharacter)) _mask = new string(SecretCharacter, length);
            return _mask;
        }
    }

    protected virtual float RightPadding => 0f;

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
        var inner = InnerRect();
        var font = GetThemeFont();
        if (font == null) return;

        float size = GetThemeFontSize();
        float lineHeight = font.GetLineHeight(size);
        float y = MathF.Floor(inner.Y + (inner.Height - lineHeight) * 0.5f);
        float origin = TextOrigin(font, size, inner);

        renderer.PushClip(inner);
        string text = DisplayText;
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
        return TextMetrics.IndexAt(font, DisplayText, localPosition.X - TextOrigin(font, size, inner), size);
    }

    protected override void EnsureCaretVisible()
    {
        var font = GetThemeFont();
        if (font == null) return;

        float size = GetThemeFontSize();
        float width = InnerRect().Width;
        string text = DisplayText;
        float caret = TextMetrics.XOf(font, text, State.Caret, size);
        if (caret - _scroll > width - 2f) _scroll = caret - width + 2f;
        if (caret - _scroll < 0f) _scroll = caret;
        float total = font.MeasureWidth(text, size);
        _scroll = Math.Clamp(_scroll, 0f, MathF.Max(0f, total - width + 2f));
    }

    protected override void OnTextReplaced()
    {
        _scroll = 0f;
    }

    private Rect2 InnerRect()
    {
        var style = GetThemeStyleBox("normal");
        var rect = style == null ? LocalRect : style.ContentRect(LocalRect);
        return new Rect2(rect.X, rect.Y, MathF.Max(0f, rect.Width - RightPadding), rect.Height);
    }

    private float TextOrigin(CanvasFont font, float size, Rect2 inner)
    {
        float x = inner.X - _scroll;
        if (Alignment == HorizontalAlignment.Left || HasFocus) return MathF.Floor(x);

        float width = font.MeasureWidth(DisplayText, size);
        if (width >= inner.Width) return MathF.Floor(x);
        return MathF.Floor(Alignment == HorizontalAlignment.Center ? inner.X + (inner.Width - width) * 0.5f : inner.End.X - width);
    }
}

internal sealed class PasteOnlyClipboard : ICanvasClipboard
{
    private readonly ICanvasClipboard _inner;

    public PasteOnlyClipboard(ICanvasClipboard inner)
    {
        _inner = inner;
    }

    public string GetText() => _inner?.GetText() ?? "";

    public void SetText(string text)
    {
    }
}
