using Engine;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Spacebox.Game.Signs;

public static class SignTextBaker
{
    public const int FontPixels = 12;

    private static readonly Dictionary<string, SKTypeface> _typefaces = new Dictionary<string, SKTypeface>(StringComparer.OrdinalIgnoreCase);

    public static PixelData Bake(IReadOnlyList<SignLine> lines, int width, int height, int margin, string fontPath, SignTextSize size)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint { IsAntialias = false })
        {
            canvas.Clear(SKColors.Transparent);
            float scale = size.Resolve(FitScale(lines, width, height, margin, fontPath));
            using var font = CreateFittingFont(lines, width, height, margin, fontPath, scale, out scale);
            DrawLines(canvas, font, paint, lines, width, height, margin, scale);
        }

        var data = new byte[width * height * 4];
        bitmap.GetPixelSpan().CopyTo(data);
        return new PixelData { Width = width, Height = height, Data = data };
    }

    public static float FitScale(IReadOnlyList<SignLine> lines, int width, int height, int margin, string fontPath)
    {
        using var font = CreateFont(fontPath, 1f);
        return FitScale(font, lines, width, height, margin, 1f);
    }

    public static float MeasureText(string text, string fontPath)
    {
        using var font = CreateFont(fontPath, 1f);
        return font.MeasureText(text ?? "");
    }

    public static float BaseLineHeight(string fontPath)
    {
        using var font = CreateFont(fontPath, 1f);
        return LineHeight(font, 1f);
    }

    private static float LineHeight(SKFont font, float scale) => MathF.Ceiling(font.Metrics.Descent - font.Metrics.Ascent) + MathF.Max(1f, MathF.Round(scale));

    private static float BoldOffset(float scale) => MathF.Max(1f, MathF.Round(scale));

    private static SKFont CreateFont(string fontPath, float scale)
    {
        return new SKFont(GetTypeface(fontPath), FontPixels * scale) { Edging = SKFontEdging.Alias, Hinting = SKFontHinting.None, Subpixel = false };
    }

    private static float FitScale(SKFont font, IReadOnlyList<SignLine> lines, int width, int height, int margin, float scale)
    {
        float availableWidth = Math.Max(1, width - margin * 2);
        float availableHeight = Math.Max(1, height - margin * 2);
        float widest = 0f;
        foreach (var line in lines) widest = MathF.Max(widest, MeasureLine(font, line, scale));
        float tallest = Math.Max(1, lines.Count) * LineHeight(font, scale);

        float fit = scale * availableHeight / tallest;
        if (widest > 0f) fit = MathF.Min(fit, scale * availableWidth / widest);
        return fit;
    }

    private static SKFont CreateFittingFont(IReadOnlyList<SignLine> lines, int width, int height, int margin, string fontPath, float scale, out float finalScale)
    {
        var font = CreateFont(fontPath, scale);
        for (int i = 0; i < 4; i++)
        {
            float fit = FitScale(font, lines, width, height, margin, scale);
            if (fit >= scale) break;
            scale = fit * 0.995f;
            font.Dispose();
            font = CreateFont(fontPath, scale);
        }
        finalScale = scale;
        return font;
    }

    private static void DrawLines(SKCanvas canvas, SKFont font, SKPaint paint, IReadOnlyList<SignLine> lines, int width, int height, int margin, float scale)
    {
        float lineHeight = LineHeight(font, scale);
        float ascent = -font.Metrics.Ascent;
        float bold = BoldOffset(scale);
        float top = MathF.Floor((height - lines.Count * lineHeight) * 0.5f);

        canvas.Save();
        canvas.ClipRect(new SKRect(margin, margin, width - margin, height - margin));
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            float lineWidth = MeasureLine(font, line, scale);
            float x = line.Align switch
            {
                SignAlign.Left => margin,
                SignAlign.Right => width - margin - lineWidth,
                _ => MathF.Floor((width - lineWidth) * 0.5f)
            };
            float baseline = MathF.Round(top + i * lineHeight + ascent);

            foreach (var run in line.Runs)
            {
                paint.Color = ToSkia(run.Color);
                canvas.DrawText(run.Text, x, baseline, SKTextAlign.Left, font, paint);
                if (run.Bold) canvas.DrawText(run.Text, x + bold, baseline, SKTextAlign.Left, font, paint);
                x += RunWidth(font, run, scale);
            }
        }
        canvas.Restore();
    }

    private static float MeasureLine(SKFont font, SignLine line, float scale)
    {
        float width = 0f;
        foreach (var run in line.Runs) width += RunWidth(font, run, scale);
        return width;
    }

    private static float RunWidth(SKFont font, SignRun run, float scale) => font.MeasureText(run.Text) + (run.Bold ? BoldOffset(scale) : 0f);

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
