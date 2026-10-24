using System.Text;
using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.GUI;

public sealed class OverlayTextControl : Control, ICanvasTextSource
{
    private const float Spacing = 6f;
    private const float SectionGap = 4f;
    private const float MinSeparator = 16f;
    private const float BarLines = 5f;

    private static readonly Color4 BarBackground = new Color4(1f, 1f, 1f, 0.12f);
    private static readonly Color4 BarFill = new Color4(0.85f, 0.6f, 0.15f, 0.85f);

    private readonly StringBuilder _line = new StringBuilder();
    private OverlayText _text;
    private int _shownVersion = -1;

    public OverlayTextControl()
    {
        MouseFilter = MouseFilter.Ignore;
    }

    public OverlayText Text
    {
        get => _text;
        set
        {
            _text = value;
            _shownVersion = -1;
            QueueLayout();
        }
    }

    public override void Process(float deltaTime)
    {
        base.Process(deltaTime);
        if (_text == null || _text.Version == _shownVersion) return;
        _shownVersion = _text.Version;
        QueueLayout();
    }

    public override Vector2 GetMinimumSize() => Walk(null, null);

    protected override void Draw(ICanvasRenderer renderer) => Walk(renderer, null);

    public void CollectText(List<CanvasTextRun> runs) => Walk(null, runs);

    private Vector2 Walk(ICanvasRenderer renderer, List<CanvasTextRun> runs)
    {
        var font = GetThemeFont();
        if (font == null || _text == null || _text.Spans.Count == 0) return Vector2.Zero;

        float size = GetThemeFontSize();
        float lineHeight = font.GetLineHeight(size);
        float x = 0f;
        float y = 0f;
        float width = 0f;
        bool first = true;
        _line.Clear();

        var spans = _text.Spans;
        for (int i = 0; i < spans.Count; i++)
        {
            var span = spans[i];
            if (span.NewLine)
            {
                if (!first)
                {
                    FlushLine(runs, x, y, lineHeight);
                    y += lineHeight;
                    if (span.Kind == OverlaySpanKind.Section) y += SectionGap;
                }
                x = 0f;
                first = false;
            }
            else
            {
                x += Spacing;
                if (runs != null) _line.Append(' ');
            }

            x += DrawSpan(renderer, font, size, lineHeight, span, x, y);
            if (runs != null) _line.Append(span.Text);
            width = MathF.Max(width, x);
        }

        FlushLine(runs, x, y, lineHeight);
        return new Vector2(MathF.Ceiling(width), MathF.Ceiling(y + lineHeight));
    }

    private float DrawSpan(ICanvasRenderer renderer, CanvasFont font, float size, float lineHeight, OverlaySpan span, float x, float y)
    {
        switch (span.Kind)
        {
            case OverlaySpanKind.Section:
            {
                float textWidth = font.MeasureWidth(span.Text, size);
                if (renderer != null)
                {
                    CanvasDraw.Text(renderer, font, new Vector2(x, y), span.Text, size, span.Color);
                    float lineY = MathF.Round(y + lineHeight * 0.5f);
                    CanvasShapes.Line(renderer, new Vector2(x + textWidth + Spacing, lineY), new Vector2(MathF.Max(x + textWidth + MinSeparator, Size.X), lineY), DarkTheme.Border);
                }
                return textWidth + MinSeparator;
            }
            case OverlaySpanKind.Bar:
            {
                float barWidth = MathF.Round(lineHeight * BarLines);
                if (renderer != null)
                {
                    var rect = new Rect2(x, y + 1f, barWidth, lineHeight - 2f);
                    renderer.DrawRect(rect, BarBackground);
                    if (span.Value > 0f) renderer.DrawRect(new Rect2(rect.X, rect.Y, MathF.Round(rect.Width * span.Value), rect.Height), BarFill);
                    if (span.Text.Length > 0)
                    {
                        float textWidth = font.MeasureWidth(span.Text, size);
                        CanvasDraw.Text(renderer, font, new Vector2(MathF.Floor(x + (barWidth - textWidth) * 0.5f), y), span.Text, size, span.Color);
                    }
                }
                return barWidth;
            }
            default:
                if (renderer != null) CanvasDraw.Text(renderer, font, new Vector2(x, y), span.Text, size, span.Color);
                return font.MeasureWidth(span.Text, size);
        }
    }

    private void FlushLine(List<CanvasTextRun> runs, float width, float y, float lineHeight)
    {
        if (runs == null || _line.Length == 0) return;
        runs.Add(new CanvasTextRun(_line.ToString(), new Rect2(GlobalPosition.X, GlobalPosition.Y + y, width, lineHeight)));
        _line.Clear();
    }
}
