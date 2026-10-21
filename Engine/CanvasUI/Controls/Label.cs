using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class Label : Control
{
    private readonly List<string> _lines = new List<string>();
    private string _text = "";
    private bool _autowrap;
    private string _wrappedText;
    private float _wrappedWidth = -1f;
    private float _wrappedSize = -1f;

    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;
    public Color4? FontColor { get; set; }
    public Color4 ShadowColor { get; set; } = new Color4(0f, 0f, 0f, 0f);
    public Vector2 ShadowOffset { get; set; } = new Vector2(1f, 1f);
    public Color4 OutlineColor { get; set; } = new Color4(0f, 0f, 0f, 0f);
    public float OutlineSize { get; set; }

    public Label()
    {
        MouseFilter = MouseFilter.Ignore;
    }

    public Label(string text) : this()
    {
        _text = text ?? "";
    }

    public string Text
    {
        get => _text;
        set
        {
            value ??= "";
            if (_text == value) return;
            _text = value;
            _wrappedText = null;
            QueueLayout();
        }
    }

    public bool Autowrap
    {
        get => _autowrap;
        set
        {
            _autowrap = value;
            _wrappedText = null;
            QueueLayout();
        }
    }

    public override Vector2 GetMinimumSize()
    {
        var font = GetThemeFont();
        if (font == null) return Vector2.Zero;
        float size = GetThemeFontSize();
        if (!_autowrap) return font.Measure(_text, size) + OutlinePadding();

        var lines = Lines(font, size, Size.X);
        return new Vector2(0f, lines.Count * font.GetLineHeight(size));
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var font = GetThemeFont();
        if (font == null || _text.Length == 0) return;

        float size = GetThemeFontSize();
        float lineHeight = font.GetLineHeight(size);
        var lines = Lines(font, size, _autowrap ? Size.X : 0f);
        float totalHeight = lines.Count * lineHeight;
        float y = VerticalAlignment == VerticalAlignment.Center ? (Size.Y - totalHeight) * 0.5f
            : VerticalAlignment == VerticalAlignment.Bottom ? Size.Y - totalHeight : 0f;
        var color = FontColor ?? GetThemeColor("font_color", Color4.White);

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            float width = font.MeasureWidth(line, size);
            float x = HorizontalAlignment == HorizontalAlignment.Center ? (Size.X - width) * 0.5f
                : HorizontalAlignment == HorizontalAlignment.Right ? Size.X - width : 0f;
            var position = new Vector2(MathF.Floor(x), MathF.Floor(y + i * lineHeight));
            DrawDecorated(renderer, font, position, line, size, color);
        }
    }

    private void DrawDecorated(ICanvasRenderer renderer, CanvasFont font, Vector2 position, string line, float size, Color4 color)
    {
        if (OutlineSize > 0f && OutlineColor.A > 0f)
        {
            float o = OutlineSize;
            CanvasDraw.Text(renderer, font, position + new Vector2(-o, 0f), line, size, OutlineColor);
            CanvasDraw.Text(renderer, font, position + new Vector2(o, 0f), line, size, OutlineColor);
            CanvasDraw.Text(renderer, font, position + new Vector2(0f, -o), line, size, OutlineColor);
            CanvasDraw.Text(renderer, font, position + new Vector2(0f, o), line, size, OutlineColor);
        }
        else if (ShadowColor.A > 0f)
        {
            CanvasDraw.Text(renderer, font, position + ShadowOffset, line, size, ShadowColor);
        }
        CanvasDraw.Text(renderer, font, position, line, size, color);
    }

    private Vector2 OutlinePadding() => OutlineSize > 0f && OutlineColor.A > 0f ? new Vector2(OutlineSize * 2f) : Vector2.Zero;

    private List<string> Lines(CanvasFont font, float size, float width)
    {
        if (ReferenceEquals(_wrappedText, _text) && _wrappedWidth == width && _wrappedSize == size) return _lines;

        _wrappedText = _text;
        _wrappedWidth = width;
        _wrappedSize = size;
        TextWrap.Wrap(font, _text, size, width, _lines);
        return _lines;
    }
}
