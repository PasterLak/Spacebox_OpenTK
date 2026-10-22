namespace Engine.CanvasUI;

public static class TextMetrics
{
    public static float XOf(CanvasFont font, string line, int index, float size)
    {
        if (font == null || string.IsNullOrEmpty(line)) return 0f;
        return font.MeasureWidth(line, 0, Math.Clamp(index, 0, line.Length), size);
    }

    public static int IndexAt(CanvasFont font, string line, float x, float size)
    {
        if (font == null || string.IsNullOrEmpty(line) || x <= 0f) return 0;

        float scale = size / font.BaseSize;
        float cursor = 0f;
        for (int i = 0; i < line.Length; i++)
        {
            float advance = font.TryGetGlyph(line[i], out var glyph) ? glyph.Advance * scale : 0f;
            if (x < cursor + advance * 0.5f) return i;
            cursor += advance;
        }
        return line.Length;
    }
}
