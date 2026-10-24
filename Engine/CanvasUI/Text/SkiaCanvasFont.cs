using OpenTK.Mathematics;
using SkiaSharp;

namespace Engine.CanvasUI;

public sealed class SkiaCanvasFont : CanvasFont, IDisposable
{
    private const int AtlasWidth = 512;
    private const int Padding = 1;

    private readonly Dictionary<char, CanvasGlyph> _glyphs = new Dictionary<char, CanvasGlyph>();
    private readonly float _baseSize;
    private readonly float _lineHeight;
    private Texture2D _atlas;
    private PixelData _pixels;
    private FilterMode _filter;

    public override float BaseSize => _baseSize;
    public override float LineHeight => _lineHeight;
    public override Texture2D Atlas => _atlas is { IsDisposed: false } ? _atlas : _atlas = CreateAtlas();

    public SkiaCanvasFont(string path, int pixelSize, bool pixelated = true, IEnumerable<(char First, char Last)> ranges = null)
    {
        _baseSize = pixelSize;
        using var typeface = (File.Exists(path) ? SKTypeface.FromFile(path) : null) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, pixelSize)
        {
            Edging = pixelated ? SKFontEdging.Alias : SKFontEdging.Antialias,
            Hinting = pixelated ? SKFontHinting.None : SKFontHinting.Normal,
            Subpixel = !pixelated
        };

        var metrics = font.Metrics;
        float ascent = -metrics.Ascent;
        float descent = metrics.Descent;
        _lineHeight = MathF.Ceiling(ascent + descent + MathF.Max(0f, metrics.Leading));
        int cellHeight = (int)MathF.Ceiling(ascent + descent) + Padding * 2;

        var chars = CollectCharacters(ranges ?? DefaultRanges(), font);
        Build(font, chars, ascent, cellHeight, pixelated);
    }

    public static IEnumerable<(char First, char Last)> DefaultRanges()
    {
        yield return ((char)32, (char)126);
        yield return ((char)160, (char)255);
        yield return ((char)0x400, (char)0x45F);
        yield return ((char)0x2013, (char)0x2014);
        yield return ((char)0x2022, (char)0x2022);
        yield return ((char)0x2026, (char)0x2026);
    }

    public override bool TryGetGlyph(char c, out CanvasGlyph glyph)
    {
        if (_glyphs.TryGetValue(c, out glyph)) return true;
        return _glyphs.TryGetValue('?', out glyph);
    }

    public void Dispose()
    {
        _atlas?.Dispose();
        _atlas = null;
        _pixels = null;
    }

    private static List<char> CollectCharacters(IEnumerable<(char First, char Last)> ranges, SKFont font)
    {
        var chars = new List<char>();
        foreach (var (first, last) in ranges)
        {
            for (int c = first; c <= last; c++)
            {
                char ch = (char)c;
                if (ch == ' ' || font.ContainsGlyph(ch)) chars.Add(ch);
            }
        }
        return chars;
    }

    private void Build(SKFont font, List<char> chars, float ascent, int cellHeight, bool pixelated)
    {
        var placements = new List<(char C, int X, int Y, int W, float Advance)>(chars.Count);
        int x = 0, y = 0;
        foreach (char c in chars)
        {
            float advance = font.MeasureText(c.ToString());
            int w = (int)MathF.Ceiling(advance) + Padding * 2;
            if (x + w > AtlasWidth)
            {
                x = 0;
                y += cellHeight;
            }
            placements.Add((c, x, y, w, advance));
            x += w;
        }

        int height = NextPowerOfTwo(y + cellHeight);
        using var bitmap = new SKBitmap(AtlasWidth, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint { Color = SKColors.White, IsAntialias = !pixelated })
        {
            canvas.Clear(SKColors.Transparent);
            foreach (var p in placements)
            {
                if (p.C != ' ') canvas.DrawText(p.C.ToString(), p.X + Padding, p.Y + Padding + ascent, SKTextAlign.Left, font, paint);
                var uv = new Rect2((float)p.X / AtlasWidth, 1f - (float)p.Y / height, (float)p.W / AtlasWidth, -(float)cellHeight / height);
                _glyphs[p.C] = new CanvasGlyph(p.Advance, new Vector2(-Padding, -Padding), new Vector2(p.W, cellHeight), uv);
            }
        }

        var data = new byte[AtlasWidth * height * 4];
        var pixels = bitmap.GetPixelSpan();
        for (int row = 0; row < height; row++)
        {
            int src = row * AtlasWidth * 4;
            int dst = (height - 1 - row) * AtlasWidth * 4;
            for (int i = 0; i < AtlasWidth * 4; i += 4)
            {
                data[dst + i] = 255;
                data[dst + i + 1] = 255;
                data[dst + i + 2] = 255;
                data[dst + i + 3] = pixels[src + i + 3];
            }
        }

        _pixels = new PixelData { Width = AtlasWidth, Height = height, Data = data };
        _filter = pixelated ? FilterMode.Nearest : FilterMode.Linear;
    }

    private Texture2D CreateAtlas()
    {
        if (_pixels == null) return null;

        var atlas = new Texture2D(_pixels, _filter);
        ResourceScopes.Own(atlas);
        return atlas;
    }

    private static int NextPowerOfTwo(int value)
    {
        int result = 1;
        while (result < value) result <<= 1;
        return result;
    }
}
