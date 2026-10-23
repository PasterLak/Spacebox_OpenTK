using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Radar;

public static class RadarTextures
{
    public static bool IsOutside(int x, int y, int width, int height, float margin = 0f)
    {
        float cx = (width - 1) * 0.5f;
        float cy = (height - 1) * 0.5f;
        float radius = MathF.Min(width, height) * 0.5f + margin;
        float dx = x - cx;
        float dy = y - cy;
        return dx * dx + dy * dy > radius * radius;
    }

    public static bool IsRingColor(Color4 color) => color.G > color.R + 0.1f && color.G > color.B + 0.1f;

    public static void ClipToCircle(Texture2D texture, bool keepRing)
    {
        if (texture == null) return;

        bool changed = false;
        for (int x = 0; x < texture.Width; x++)
        {
            for (int y = 0; y < texture.Height; y++)
            {
                var color = texture.GetPixel(x, y);
                if (color.A <= 0f) continue;
                bool clear = keepRing ? !IsRingColor(color) : IsOutside(x, y, texture.Width, texture.Height);
                if (!clear) continue;

                texture.SetPixel(x, y, new Color4(color.R, color.G, color.B, 0f));
                changed = true;
            }
        }

        if (changed) texture.UpdateTexture();
    }
}
