using ImGuiNET;
using Spacebox.Game.GUI;
using System.Numerics;

namespace Spacebox.Game.Tutorial.UI;

public static class TutorialDraw
{
    public static readonly uint PanelBackground = Rgba(16, 20, 28, 215);
    public static readonly uint PanelBorder = Rgba(90, 100, 120, 255);
    public static readonly uint Gold = Rgba(255, 196, 40, 255);
    public static readonly uint Green = Rgba(110, 220, 110, 255);
    public static readonly uint DarkGreen = Rgba(40, 120, 50, 255);
    public static readonly uint Cyan = Rgba(110, 210, 230, 255);
    public static readonly uint TextBright = Rgba(240, 240, 240, 255);
    public static readonly uint TextDim = Rgba(185, 190, 200, 255);
    public static readonly uint TextFaint = Rgba(130, 135, 145, 255);
    public static readonly uint BarBackground = Rgba(45, 50, 62, 255);
    public static readonly uint BoxBorder = Rgba(170, 175, 185, 255);

    public static uint Rgba(int r, int g, int b, int a)
    {
        return (uint)((a & 0xFF) << 24 | (b & 0xFF) << 16 | (g & 0xFF) << 8 | (r & 0xFF));
    }

    public static uint WithAlpha(uint color, float alpha)
    {
        uint a = (uint)Math.Clamp((color >> 24) * alpha, 0f, 255f);
        return (color & 0x00FFFFFF) | (a << 24);
    }

    public static uint Lerp(uint from, uint to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        uint result = 0;
        for (int shift = 0; shift < 32; shift += 8)
        {
            float a = (from >> shift) & 0xFF;
            float b = (to >> shift) & 0xFF;
            result |= (uint)MathF.Round(a + (b - a) * t) << shift;
        }
        return result;
    }

    public static float Measure(ImFontPtr font, float size, string text)
    {
        return string.IsNullOrEmpty(text) ? 0f : font.CalcTextSizeA(size, float.MaxValue, 0f, text).X;
    }

    public static void Wrap(ImFontPtr font, float size, string text, float width, List<string> lines)
    {
        lines.Clear();
        if (string.IsNullOrEmpty(text)) return;

        foreach (var paragraph in text.Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string line = "";
            foreach (var word in words)
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && Measure(font, size, candidate) > width)
                {
                    lines.Add(line);
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }

            lines.Add(line);
        }
    }

    public static string Truncate(ImFontPtr font, float size, string text, float width)
    {
        if (Measure(font, size, text) <= width) return text;

        for (int length = text.Length - 1; length > 0; length--)
        {
            string candidate = text.Substring(0, length) + "...";
            if (Measure(font, size, candidate) <= width) return candidate;
        }
        return "...";
    }

    public static void Diamond(ImDrawListPtr drawList, Vector2 center, float radius, uint color)
    {
        drawList.AddQuadFilled(
            center + new Vector2(0, -radius),
            center + new Vector2(radius, 0),
            center + new Vector2(0, radius),
            center + new Vector2(-radius, 0),
            color);
    }

    public static void CheckBox(ImDrawListPtr drawList, Vector2 min, float size, bool done, bool optional, float alpha)
    {
        var max = min + new Vector2(size);
        if (done)
        {
            drawList.AddRectFilled(min, max, WithAlpha(Green, alpha), size * 0.15f);
            float thickness = MathF.Max(1.5f, size * 0.14f);
            uint mark = WithAlpha(Rgba(20, 50, 20, 255), alpha);
            drawList.AddLine(min + new Vector2(size * 0.2f, size * 0.52f), min + new Vector2(size * 0.42f, size * 0.75f), mark, thickness);
            drawList.AddLine(min + new Vector2(size * 0.42f, size * 0.75f), min + new Vector2(size * 0.8f, size * 0.25f), mark, thickness);
            return;
        }

        drawList.AddRect(min, max, WithAlpha(optional ? Cyan : BoxBorder, alpha), size * 0.15f, ImDrawFlags.None, MathF.Max(1f, size * 0.08f));
    }

    public static void Icon(ImDrawListPtr drawList, TutorialIcon icon, Vector2 min, float size, float alpha)
    {
        if (icon.IsEmpty) return;

        var sprite = icon.IsItem
            ? GameAssets.TryGetItemById(icon.ItemId, out var item) && item != null ? item.Icon : Engine.Graphics.Atlas.AtlasSprite.None
            : UIAtlas.Get(icon.UiName);
        sprite.Draw(drawList, min, min + new Vector2(size), WithAlpha(0xFFFFFFFF, alpha));
    }

    public static float EaseOut(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return 1f - (1f - t) * (1f - t);
    }
}
