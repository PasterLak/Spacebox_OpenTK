using Engine;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Spacebox.Game.Signs;

public static class SignTextBaker
{
    public const int FontPixels = 12;

    private static readonly Dictionary<string, SKTypeface> _typefaces = new Dictionary<string, SKTypeface>(StringComparer.OrdinalIgnoreCase);

    public static PixelData Bake(IReadOnlyList<SignLine> lines, int width, int height, int margin, string fontPath)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using (var canvas = new SKCanvas(bitmap))
        using (var font = new SKFont(GetTypeface(fontPath), FontPixels) { Edging = SKFontEdging.Alias, Hinting = SKFontHinting.None, Subpixel = false })
        using (var paint = new SKPaint { IsAntialias = false })
        {
            canvas.Clear(SKColors.Transparent);
            DrawLines(canvas, font, paint, lines, width, height, margin);
        }

        var data = new byte[width * height * 4];
        bitmap.GetPixelSpan().CopyTo(data);
        return new PixelData { Width = width, Height = height, Data = data };
    }

    public static int LineHeight(SKFont font) => (int)MathF.Ceiling(font.Metrics.Descent - font.Metrics.Ascent) + 1;

    private static void DrawLines(SKCanvas canvas, SKFont font, SKPaint paint, IReadOnlyList<SignLine> lines, int width, int height, int margin)
    {
        int lineHeight = LineHeight(font);
        float ascent = -font.Metrics.Ascent;
        int maxLines = Math.Max(1, (height - margin * 2) / lineHeight);
        int count = Math.Min(lines.Count, maxLines);
        float top = MathF.Floor((height - count * lineHeight) * 0.5f);

        for (int i = 0; i < count; i++)
        {
            var line = lines[i];
            float lineWidth = MeasureLine(font, line);
            float x = line.Align switch
            {
                SignAlign.Left => margin,
                SignAlign.Right => width - margin - lineWidth,
                _ => MathF.Floor((width - lineWidth) * 0.5f)
            };
            float baseline = top + i * lineHeight + ascent;

            foreach (var run in line.Runs)
            {
                paint.Color = ToSkia(run.Color);
                canvas.DrawText(run.Text, x, baseline, SKTextAlign.Left, font, paint);
                if (run.Bold) canvas.DrawText(run.Text, x + 1f, baseline, SKTextAlign.Left, font, paint);
                x += RunWidth(font, run);
            }
        }
    }

    private static float MeasureLine(SKFont font, SignLine line)
    {
        float width = 0f;
        foreach (var run in line.Runs) width += RunWidth(font, run);
        return width;
    }

    private static float RunWidth(SKFont font, SignRun run) => font.MeasureText(run.Text) + (run.Bold ? 1f : 0f);

    private static SKColor ToSkia(Color4 color)
    {
        return new SKColor((byte)(Math.Clamp(color.R, 0f, 1f) * 255f), (byte)(Math.Clamp(color.G, 0f, 1f) * 255f), (byte)(Math.Clamp(color.B, 0f, 1f) * 255f), 255);
    }

    private static SKTypeface GetTypeface(string path)
    {
        path ??= "";
        if (_typefaces.TryGetValue(path, out var typeface)) return typeface;

        typeface = (File.Exists(path) ? SKTypeface.FromFile(path) : null) ?? SKTypeface.Default;
        if (!File.Exists(path)) Debug.Warning($"[Signs] Font '{path}' was not found, using the default font");
        _typefaces[path] = typeface;
        return typeface;
    }
}
