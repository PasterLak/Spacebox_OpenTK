using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class ColorRect : Control
{
    public Color4 Color { get; set; } = Color4.White;

    public ColorRect()
    {
        MouseFilter = MouseFilter.Ignore;
    }

    public ColorRect(Color4 color) : this()
    {
        Color = color;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, Color);
    }
}

public class Panel : Control
{
    public StyleBox Style => GetThemeStyleBox("panel");

    protected override void Draw(ICanvasRenderer renderer)
    {
        Style?.Draw(renderer, LocalRect);
    }
}

public enum TextureStretchMode
{
    Scale,
    Tile,
    Keep,
    KeepCentered,
    KeepAspect,
    KeepAspectCentered,
    KeepAspectCovered
}

public class TextureRect : Control
{
    private Texture2D _texture;
    private Rect2? _region;

    public TextureStretchMode StretchMode { get; set; } = TextureStretchMode.KeepAspectCentered;
    public bool IgnoreTextureSize { get; set; } = true;
    public bool FlipH { get; set; }
    public bool FlipV { get; set; }

    public TextureRect()
    {
        MouseFilter = MouseFilter.Pass;
    }

    public Texture2D Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            QueueLayout();
        }
    }

    public Rect2? Region
    {
        get => _region;
        set
        {
            _region = value;
            QueueLayout();
        }
    }

    public Vector2 TextureSize => _region?.Size ?? (_texture == null ? Vector2.Zero : new Vector2(_texture.Width, _texture.Height));

    public override Vector2 GetMinimumSize() => IgnoreTextureSize ? Vector2.Zero : TextureSize;

    protected override void Draw(ICanvasRenderer renderer)
    {
        if (_texture == null) return;
        var textureSize = TextureSize;
        if (textureSize.X <= 0f || textureSize.Y <= 0f) return;

        var uv = CanvasDraw.UvOf(_texture, _region, FlipH, FlipV);
        if (StretchMode == TextureStretchMode.Tile)
        {
            DrawTiled(renderer, textureSize, uv);
            return;
        }

        renderer.DrawTexture(_texture, Fit(textureSize), uv, Color4.White);
    }

    private Rect2 Fit(Vector2 textureSize)
    {
        var size = Size;
        switch (StretchMode)
        {
            case TextureStretchMode.Keep:
                return new Rect2(Vector2.Zero, textureSize);
            case TextureStretchMode.KeepCentered:
                return new Rect2(Floor((size - textureSize) * 0.5f), textureSize);
            case TextureStretchMode.KeepAspect:
            case TextureStretchMode.KeepAspectCentered:
            case TextureStretchMode.KeepAspectCovered:
                float sx = size.X / textureSize.X;
                float sy = size.Y / textureSize.Y;
                float scale = StretchMode == TextureStretchMode.KeepAspectCovered ? MathF.Max(sx, sy) : MathF.Min(sx, sy);
                var fitted = textureSize * scale;
                var offset = StretchMode == TextureStretchMode.KeepAspect ? Vector2.Zero : (size - fitted) * 0.5f;
                return new Rect2(Floor(offset), Floor(fitted));
            default:
                return LocalRect;
        }
    }

    private void DrawTiled(ICanvasRenderer renderer, Vector2 textureSize, Rect2 uv)
    {
        for (float y = 0f; y < Size.Y; y += textureSize.Y)
        {
            for (float x = 0f; x < Size.X; x += textureSize.X)
            {
                float w = MathF.Min(textureSize.X, Size.X - x);
                float h = MathF.Min(textureSize.Y, Size.Y - y);
                var partUv = new Rect2(uv.X, uv.Y, uv.Width * w / textureSize.X, uv.Height * h / textureSize.Y);
                renderer.DrawTexture(_texture, new Rect2(x, y, w, h), partUv, Color4.White);
            }
        }
    }

    private static Vector2 Floor(Vector2 v) => new Vector2(MathF.Floor(v.X), MathF.Floor(v.Y));
}

public class NinePatchRect : Control
{
    public Texture2D Texture { get; set; }
    public Rect2? Region { get; set; }
    public float PatchLeft { get; set; }
    public float PatchTop { get; set; }
    public float PatchRight { get; set; }
    public float PatchBottom { get; set; }
    public bool DrawCenter { get; set; } = true;

    public NinePatchRect()
    {
        MouseFilter = MouseFilter.Pass;
    }

    public NinePatchRect SetPatchMargins(float all)
    {
        PatchLeft = PatchTop = PatchRight = PatchBottom = all;
        return this;
    }

    public override Vector2 GetMinimumSize() => new Vector2(PatchLeft + PatchRight, PatchTop + PatchBottom);

    protected override void Draw(ICanvasRenderer renderer)
    {
        CanvasDraw.NinePatch(renderer, Texture, LocalRect, Region, PatchLeft, PatchTop, PatchRight, PatchBottom, Color4.White, DrawCenter);
    }
}
