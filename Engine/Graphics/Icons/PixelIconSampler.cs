using OpenTK.Mathematics;

namespace Engine.Graphics.Icons;

public static class PixelIconSampler
{
    public static IconSampler FromTopDown(Color4[,] pixels, bool uvOriginTop = false)
    {
        int width = pixels.GetLength(0);
        int height = pixels.GetLength(1);

        return (Vector2 uv, Vector2 anchor, out Color4 color) =>
        {
            color = default;
            if (width == 0 || height == 0) return false;

            float u = uv.X - MathF.Floor(uv.X);
            float v = uv.Y - MathF.Floor(uv.Y);
            int x = Math.Clamp((int)(u * width), 0, width - 1);
            int y = Math.Clamp((int)((uvOriginTop ? v : 1f - v) * height), 0, height - 1);
            color = pixels[x, y];
            return true;
        };
    }
}
