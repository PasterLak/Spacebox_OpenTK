using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public readonly struct CanvasGlyph
{
    public readonly float Advance;
    public readonly Vector2 Offset;
    public readonly Vector2 Size;
    public readonly Rect2 Uv;

    public CanvasGlyph(float advance, Vector2 offset, Vector2 size, Rect2 uv)
    {
        Advance = advance;
        Offset = offset;
        Size = size;
        Uv = uv;
    }
}

public abstract class CanvasFont
{
    public abstract float BaseSize { get; }
    public abstract float LineHeight { get; }
    public abstract Texture2D Atlas { get; }
    public abstract bool TryGetGlyph(char c, out CanvasGlyph glyph);

    public float GetLineHeight(float size) => LineHeight * size / BaseSize;

    public float MeasureWidth(string text, float size)
    {
        if (string.IsNullOrEmpty(text)) return 0f;
        return MeasureWidth(text, 0, text.Length, size);
    }

    public float MeasureWidth(string text, int start, int length, float size)
    {
        float width = 0f;
        float line = 0f;
        int end = Math.Min(text.Length, start + length);
        for (int i = start; i < end; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                width = MathF.Max(width, line);
                line = 0f;
                continue;
            }
            if (TryGetGlyph(c, out var glyph)) line += glyph.Advance;
        }
        return MathF.Max(width, line) * size / BaseSize;
    }

    public Vector2 Measure(string text, float size)
    {
        if (string.IsNullOrEmpty(text)) return new Vector2(0f, GetLineHeight(size));
        int lines = 1;
        for (int i = 0; i < text.Length; i++) if (text[i] == '\n') lines++;
        return new Vector2(MeasureWidth(text, size), lines * GetLineHeight(size));
    }
}
