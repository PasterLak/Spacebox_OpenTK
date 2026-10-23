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
    public float CornerRadius { get; set; }

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

    public StyleBoxFlat SetCornerRadius(float radius)
    {
        CornerRadius = radius;
        return this;
    }

    public override void Draw(ICanvasRenderer renderer, Rect2 rect)
    {
        if (CornerRadius > 0f)
        {
            DrawRounded(renderer, rect);
            return;
        }

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

    private void DrawRounded(ICanvasRenderer renderer, Rect2 rect)
    {
        if (ShadowColor.A > 0f && ShadowOffset != Vector2.Zero)
            FillRounded(renderer, new Rect2(rect.Position + ShadowOffset, rect.Size), CornerRadius, ShadowColor);

        float border = MathF.Max(MathF.Max(BorderLeft, BorderRight), MathF.Max(BorderTop, BorderBottom));
        bool hasBorder = border > 0f && BorderColor.A > 0f;
        if (hasBorder) FillRounded(renderer, rect, CornerRadius, BorderColor);

        var inner = hasBorder ? rect.Shrink(BorderLeft, BorderTop, BorderRight, BorderBottom) : rect;
        float innerRadius = MathF.Max(0f, CornerRadius - border);
        if (DrawCenter && BgColor.A > 0f) FillRounded(renderer, inner, innerRadius, BgColor);
        if (HighlightSize > 0f && HighlightColor.A > 0f)
            renderer.DrawRect(new Rect2(inner.X + innerRadius, inner.Y, MathF.Max(0f, inner.Width - innerRadius * 2f), HighlightSize), HighlightColor);
    }

    public static void FillRounded(ICanvasRenderer renderer, Rect2 rect, float radius, Color4 color)
    {
        float r = MathF.Min(radius, MathF.Min(rect.Width, rect.Height) * 0.5f);
        int rows = (int)MathF.Ceiling(r);
        if (rows <= 0)
        {
            renderer.DrawRect(rect, color);
            return;
        }

        if (rect.Height - rows * 2f > 0f) renderer.DrawRect(new Rect2(rect.X, rect.Y + rows, rect.Width, rect.Height - rows * 2f), color);
        for (int i = 0; i < rows; i++)
        {
            float inset = CornerInset(r, i);
            float width = rect.Width - inset * 2f;
            if (width <= 0f) continue;
            renderer.DrawRect(new Rect2(rect.X + inset, rect.Y + i, width, 1f), color);
            renderer.DrawRect(new Rect2(rect.X + inset, rect.Y + rect.Height - 1f - i, width, 1f), color);
        }
    }

    public static float CornerInset(float radius, int row)
    {
        float dy = radius - (row + 0.5f);
        return MathF.Round(radius - MathF.Sqrt(MathF.Max(0f, radius * radius - dy * dy)));
    }
}
