using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public enum FillMode
{
    LeftToRight,
    RightToLeft,
    BottomToTop,
    TopToBottom
}

public class ProgressBar : Control
{
    private float _value;
    private int _percent = -1;
    private string _percentText = "";

    public float MinValue { get; set; }
    public float MaxValue { get; set; } = 100f;
    public FillMode FillMode { get; set; } = FillMode.LeftToRight;
    public bool ShowPercentage { get; set; } = true;
    public Color4? FillColor { get; set; }
    public string Text { get; set; }

    public ProgressBar()
    {
        MouseFilter = MouseFilter.Pass;
    }

    public float Value
    {
        get => _value;
        set => _value = Math.Clamp(value, MinValue, MaxValue);
    }

    public float Ratio => MaxValue > MinValue ? (_value - MinValue) / (MaxValue - MinValue) : 0f;

    public override Vector2 GetMinimumSize()
    {
        var background = GetThemeStyleBox("background");
        var min = background?.MinimumSize ?? Vector2.Zero;
        var font = GetThemeFont();
        if (font != null && (ShowPercentage || !string.IsNullOrEmpty(Text)))
            min.Y = MathF.Max(min.Y, font.GetLineHeight(GetThemeFontSize()));
        return min;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var rect = LocalRect;
        GetThemeStyleBox("background")?.Draw(renderer, rect);

        float ratio = Ratio;
        if (ratio > 0f)
        {
            if (FillColor.HasValue) renderer.DrawRect(FillRect(rect, ratio), FillColor.Value);
            else GetThemeStyleBox("fill")?.Draw(renderer, FillRect(rect, ratio));
        }

        string label = !string.IsNullOrEmpty(Text) ? Text : ShowPercentage ? PercentText(ratio) : null;
        if (string.IsNullOrEmpty(label)) return;

        var font = GetThemeFont();
        if (font == null) return;
        float size = GetThemeFontSize();
        var textSize = font.Measure(label, size);
        var position = new Vector2(MathF.Floor((rect.Width - textSize.X) * 0.5f), MathF.Floor((rect.Height - textSize.Y) * 0.5f));
        var outline = GetThemeColor("font_outline_color", new Color4(0f, 0f, 0f, 0f));
        if (outline.A > 0f) CanvasDraw.Text(renderer, font, position + Vector2.One, label, size, outline);
        CanvasDraw.Text(renderer, font, position, label, size, GetThemeColor("font_color", Color4.White));
    }

    private string PercentText(float ratio)
    {
        int percent = (int)MathF.Round(ratio * 100f);
        if (percent != _percent)
        {
            _percent = percent;
            _percentText = percent + "%";
        }
        return _percentText;
    }

    private Rect2 FillRect(Rect2 rect, float ratio)
    {
        return FillMode switch
        {
            FillMode.RightToLeft => new Rect2(rect.End.X - rect.Width * ratio, rect.Y, rect.Width * ratio, rect.Height),
            FillMode.BottomToTop => new Rect2(rect.X, rect.End.Y - rect.Height * ratio, rect.Width, rect.Height * ratio),
            FillMode.TopToBottom => new Rect2(rect.X, rect.Y, rect.Width, rect.Height * ratio),
            _ => new Rect2(rect.X, rect.Y, MathF.Round(rect.Width * ratio), rect.Height)
        };
    }
}
