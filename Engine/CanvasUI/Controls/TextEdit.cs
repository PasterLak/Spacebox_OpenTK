using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class TextEdit : TextEditBase
{
    private readonly List<int> _lineStarts = new List<int> { 0 };
    private string _indexedText;
    private Vector2 _scroll;

    public TextEdit() : base(true)
    {
    }

    public int VisibleLinesMinimum { get; set; } = 3;
    public bool ShowLineNumbers { get; set; }
    public int LineCount
    {
        get
        {
            IndexLines();
            return _lineStarts.Count;
        }
    }

    public override Vector2 GetMinimumSize()
    {
        var font = GetThemeFont();
        float lineHeight = font?.GetLineHeight(GetThemeFontSize()) ?? 12f;
        var style = GetThemeStyleBox("normal");
        return new Vector2(40f, lineHeight * Math.Max(1, VisibleLinesMinimum)) + (style?.MinimumSize ?? Vector2.Zero);
    }

    protected internal override bool OnScroll(Vector2 localPosition, float delta)
    {
        var font = GetThemeFont();
        if (font == null) return false;

        float lineHeight = font.GetLineHeight(GetThemeFontSize());
        float max = MaxScrollY(lineHeight);
        if (max <= 0f) return false;

        _scroll.Y = Math.Clamp(_scroll.Y - delta * lineHeight * 3f, 0f, max);
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = CurrentStyle();
        style?.Draw(renderer, LocalRect);
        var font = GetThemeFont();
        if (font == null) return;

        IndexLines();
        float size = GetThemeFontSize();
        float lineHeight = font.GetLineHeight(size);
        var inner = InnerRect();
        float gutter = Gutter(font, size);
        var textRect = new Rect2(inner.X + gutter, inner.Y, inner.Width - gutter, inner.Height);
        _scroll.Y = Math.Clamp(_scroll.Y, 0f, MaxScrollY(lineHeight));

        int first = Math.Max(0, (int)MathF.Floor(_scroll.Y / lineHeight));
        int last = Math.Min(_lineStarts.Count - 1, (int)MathF.Ceiling((_scroll.Y + inner.Height) / lineHeight));
        string text = State.Text;
        var color = GetThemeColor("font_color", Color4.White);

        if (gutter > 0f)
        {
            renderer.PushClip(inner);
            var numberColor = GetThemeColor("line_number_color", new Color4(1f, 1f, 1f, 0.3f));
            for (int line = first; line <= last; line++)
            {
                float y = MathF.Floor(inner.Y + line * lineHeight - _scroll.Y);
                CanvasDraw.Text(renderer, font, new Vector2(inner.X, y), (line + 1).ToString(), size, numberColor);
            }
            renderer.PopClip();
        }

        renderer.PushClip(textRect);
        if (text.Length == 0 && !HasFocus && PlaceholderText.Length > 0)
            CanvasDraw.Text(renderer, font, new Vector2(textRect.X, textRect.Y), PlaceholderText, size, GetThemeColor("placeholder_color", new Color4(1f, 1f, 1f, 0.35f)));

        for (int line = first; line <= last; line++)
        {
            string content = LineText(line);
            float y = MathF.Floor(textRect.Y + line * lineHeight - _scroll.Y);
            float x = MathF.Floor(textRect.X - _scroll.X);
            DrawLineSelection(renderer, font, size, line, content, x, y, lineHeight);
            CanvasDraw.Text(renderer, font, new Vector2(x, y), content, size, color);
        }

        var (caretLine, caretColumn) = Locate(State.Caret);
        float caretX = textRect.X - _scroll.X + TextMetrics.XOf(font, LineText(caretLine), caretColumn, size);
        float caretY = textRect.Y + caretLine * lineHeight - _scroll.Y;
        DrawCaret(renderer, new Vector2(caretX, caretY), lineHeight);
        renderer.PopClip();
    }

    protected override int IndexAtPoint(Vector2 localPosition)
    {
        var font = GetThemeFont();
        if (font == null) return 0;

        IndexLines();
        float size = GetThemeFontSize();
        float lineHeight = font.GetLineHeight(size);
        var inner = InnerRect();
        int line = Math.Clamp((int)MathF.Floor((localPosition.Y - inner.Y + _scroll.Y) / lineHeight), 0, _lineStarts.Count - 1);
        float x = localPosition.X - inner.X - Gutter(font, size) + _scroll.X;
        return _lineStarts[line] + TextMetrics.IndexAt(font, LineText(line), x, size);
    }

    protected override void EnsureCaretVisible()
    {
        var font = GetThemeFont();
        if (font == null) return;

        IndexLines();
        float size = GetThemeFontSize();
        float lineHeight = font.GetLineHeight(size);
        var inner = InnerRect();
        float width = inner.Width - Gutter(font, size);
        var (line, column) = Locate(State.Caret);

        float top = line * lineHeight;
        if (top < _scroll.Y) _scroll.Y = top;
        if (top + lineHeight > _scroll.Y + inner.Height) _scroll.Y = top + lineHeight - inner.Height;

        float x = TextMetrics.XOf(font, LineText(line), column, size);
        if (x < _scroll.X) _scroll.X = x;
        if (x > _scroll.X + width - 2f) _scroll.X = x - width + 2f;
        _scroll = Vector2.ComponentMax(_scroll, Vector2.Zero);
    }

    protected override void OnTextReplaced()
    {
        _scroll = Vector2.Zero;
        _indexedText = null;
    }

    protected override int PageLines()
    {
        var font = GetThemeFont();
        if (font == null) return 10;
        return Math.Max(1, (int)(InnerRect().Height / font.GetLineHeight(GetThemeFontSize())) - 1);
    }

    private void DrawLineSelection(ICanvasRenderer renderer, CanvasFont font, float size, int line, string content, float x, float y, float lineHeight)
    {
        if (!State.HasSelection || !HasFocus) return;

        int lineStart = _lineStarts[line];
        int lineEnd = lineStart + content.Length;
        int start = Math.Max(State.SelectionStart, lineStart);
        int end = Math.Min(State.SelectionEnd, lineEnd);
        bool includesBreak = State.SelectionEnd > lineEnd && State.SelectionStart <= lineEnd;
        if (start > end || (start == end && !includesBreak)) return;

        float x0 = x + TextMetrics.XOf(font, content, start - lineStart, size);
        float x1 = x + TextMetrics.XOf(font, content, end - lineStart, size) + (includesBreak ? size * 0.4f : 0f);
        DrawSelection(renderer, new Rect2(x0, y, x1 - x0, lineHeight));
    }

    private (int Line, int Column) Locate(int position)
    {
        IndexLines();
        int line = _lineStarts.BinarySearch(position);
        if (line < 0) line = ~line - 1;
        line = Math.Clamp(line, 0, _lineStarts.Count - 1);
        return (line, position - _lineStarts[line]);
    }

    private string LineText(int line)
    {
        string text = State.Text;
        int start = _lineStarts[line];
        int end = line + 1 < _lineStarts.Count ? _lineStarts[line + 1] - 1 : text.Length;
        return text.Substring(start, Math.Max(0, end - start));
    }

    private void IndexLines()
    {
        string text = State.Text;
        if (ReferenceEquals(_indexedText, text)) return;

        _indexedText = text;
        _lineStarts.Clear();
        _lineStarts.Add(0);
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n') _lineStarts.Add(i + 1);
    }

    private float MaxScrollY(float lineHeight) => MathF.Max(0f, _lineStarts.Count * lineHeight - InnerRect().Height);

    private float Gutter(CanvasFont font, float size) => ShowLineNumbers ? font.MeasureWidth(_lineStarts.Count.ToString(), size) + 8f : 0f;

    private Rect2 InnerRect()
    {
        var style = GetThemeStyleBox("normal");
        return style == null ? LocalRect : style.ContentRect(LocalRect);
    }
}
