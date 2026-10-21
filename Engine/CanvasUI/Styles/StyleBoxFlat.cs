using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class StyleBoxFlat : StyleBox
{
    public Color4 BgColor { get; set; } = new Color4(0.6f, 0.6f, 0.6f, 1f);
    public Color4 BorderColor { get; set; } = new Color4(0.8f, 0.8f, 0.8f, 1f);
    public float BorderLeft { get; set; }
    public float BorderTop { get; set; }
    public float BorderRight { get; set; }
    public float BorderBottom { get; set; }
    public bool DrawCenter { get; set; } = true;
    public Color4 ShadowColor { get; set; } = new Color4(0f, 0f, 0f, 0f);
    public Vector2 ShadowOffset { get; set; }
    public Color4 HighlightColor { get; set; } = new Color4(0f, 0f, 0f, 0f);
    public float HighlightSize { get; set; }

    public StyleBoxFlat()
    {
    }

    public StyleBoxFlat(Color4 background, float margin = 0f)
    {
        BgColor = background;
        SetContentMargins(margin);
    }

    public StyleBoxFlat SetBorder(float width, Color4 color)
    {
        BorderLeft = BorderTop = BorderRight = BorderBottom = width;
        BorderColor = color;
        return this;
    }

    public StyleBoxFlat Clone()
    {
        return (StyleBoxFlat)MemberwiseClone();
    }

    public override void Draw(ICanvasRenderer renderer, Rect2 rect)
    {
        if (ShadowColor.A > 0f && ShadowOffset != Vector2.Zero)
            renderer.DrawRect(new Rect2(rect.Position + ShadowOffset, rect.Size), ShadowColor);

        if (DrawCenter && BgColor.A > 0f)
            renderer.DrawRect(rect.Shrink(BorderLeft, BorderTop, BorderRight, BorderBottom), BgColor);

        if (HighlightSize > 0f && HighlightColor.A > 0f)
        {
            var inner = rect.Shrink(BorderLeft, BorderTop, BorderRight, BorderBottom);
            renderer.DrawRect(new Rect2(inner.X, inner.Y, inner.Width, HighlightSize), HighlightColor);
        }

        CanvasDraw.Outline(renderer, rect, BorderColor, BorderLeft, BorderTop, BorderRight, BorderBottom);
    }
}
