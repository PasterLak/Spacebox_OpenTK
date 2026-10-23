using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public static class CanvasDraw
{
    public static void Outline(ICanvasRenderer renderer, Rect2 rect, Color4 color, float left, float top, float right, float bottom)
    {
        if (color.A <= 0f) return;
        if (top > 0f) renderer.DrawRect(new Rect2(rect.X, rect.Y, rect.Width, top), color);
        if (bottom > 0f) renderer.DrawRect(new Rect2(rect.X, rect.End.Y - bottom, rect.Width, bottom), color);
        float innerHeight = rect.Height - top - bottom;
        if (innerHeight <= 0f) return;
        if (left > 0f) renderer.DrawRect(new Rect2(rect.X, rect.Y + top, left, innerHeight), color);
        if (right > 0f) renderer.DrawRect(new Rect2(rect.End.X - right, rect.Y + top, right, innerHeight), color);
    }

    public static Rect2 UvOf(Texture2D texture, Rect2? region, bool flipH = false, bool flipV = false)
    {
        float w = MathF.Max(1, texture.Width);
        float h = MathF.Max(1, texture.Height);
        var r = region ?? new Rect2(0, 0, w, h);
        float u0 = r.X / w;
        float u1 = r.End.X / w;
        float v0 = 1f - r.Y / h;
        float v1 = 1f - r.End.Y / h;
        if (flipH) (u0, u1) = (u1, u0);
        if (flipV) (v0, v1) = (v1, v0);
        return new Rect2(u0, v0, u1 - u0, v1 - v0);
    }

    public static void NinePatch(ICanvasRenderer renderer, Texture2D texture, Rect2 rect, Rect2? region,
        float left, float top, float right, float bottom, Color4 color, bool drawCenter = true)
    {
        if (texture == null) return;
        var src = region ?? new Rect2(0, 0, texture.Width, texture.Height);

        float scaleX = rect.Width < left + right ? rect.Width / MathF.Max(1f, left + right) : 1f;
        float scaleY = rect.Height < top + bottom ? rect.Height / MathF.Max(1f, top + bottom) : 1f;
        float dl = left * scaleX, dr = right * scaleX, dt = top * scaleY, db = bottom * scaleY;

        Span<float> sx = stackalloc float[] { src.X, src.X + left, src.End.X - right, src.End.X };
        Span<float> sy = stackalloc float[] { src.Y, src.Y + top, src.End.Y - bottom, src.End.Y };
        Span<float> dx = stackalloc float[] { rect.X, rect.X + dl, rect.End.X - dr, rect.End.X };
        Span<float> dy = stackalloc float[] { rect.Y, rect.Y + dt, rect.End.Y - db, rect.End.Y };

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (!drawCenter && x == 1 && y == 1) continue;
                var dest = new Rect2(dx[x], dy[y], dx[x + 1] - dx[x], dy[y + 1] - dy[y]);
                if (!dest.HasArea) continue;
                var part = new Rect2(sx[x], sy[y], sx[x + 1] - sx[x], sy[y + 1] - sy[y]);
                if (!part.HasArea) continue;
                renderer.DrawTexture(texture, dest, UvOf(texture, part), color);
            }
        }
    }

    public static void Text(ICanvasRenderer renderer, CanvasFont font, Vector2 position, string text, float size, Color4 color, float letterSpacing = 0f)
    {
        if (font == null || string.IsNullOrEmpty(text) || font.Atlas == null) return;

        float scale = size / font.BaseSize;
        float x = position.X;
        float y = position.Y;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                x = position.X;
                y += font.LineHeight * scale;
                continue;
            }

            if (!font.TryGetGlyph(c, out var glyph)) continue;
            if (glyph.Size.X > 0f && glyph.Size.Y > 0f)
            {
                var dest = new Rect2(x + glyph.Offset.X * scale, y + glyph.Offset.Y * scale, glyph.Size.X * scale, glyph.Size.Y * scale);
                renderer.DrawTexture(font.Atlas, dest, glyph.Uv, color);
            }
            x += glyph.Advance * scale + letterSpacing;
        }
    }
}
