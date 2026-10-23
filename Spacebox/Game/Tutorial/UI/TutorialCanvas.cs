using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using Spacebox.Game.GUI.Canvas;
using System.Numerics;
using Color4 = OpenTK.Mathematics.Color4;
using GlVector2 = OpenTK.Mathematics.Vector2;

namespace Spacebox.Game.Tutorial.UI;

public sealed class TutorialCanvas
{
    private const float BaseFontSize = 12f;

    public ICanvasRenderer Renderer { get; private set; }
    public CanvasFont Font { get; private set; }

    public void Begin(ICanvasRenderer renderer, CanvasFont font)
    {
        Renderer = renderer;
        Font = font;
    }

    public static float SnapSize(float size) => size <= BaseFontSize * 1.5f ? BaseFontSize : MathF.Round(size / BaseFontSize) * BaseFontSize;

    public float Measure(float size, string text) => TutorialDraw.Measure(Font, size, text);

    public void Text(float size, Vector2 position, uint color, string text)
    {
        if (Font == null || string.IsNullOrEmpty(text)) return;
        CanvasDraw.Text(Renderer, Font, new GlVector2(MathF.Round(position.X), MathF.Round(position.Y)), text, SnapSize(size), ToColor(color));
    }

    public void RectFilled(Vector2 min, Vector2 max, uint color, float rounding = 0f)
    {
        var rect = ToRect(min, max);
        rounding = MathF.Min(rounding, MathF.Min(rect.Width, rect.Height) * 0.5f);
        if (rounding > 0f) StyleBoxFlat.FillRounded(Renderer, rect, rounding, ToColor(color));
        else Renderer.DrawRect(rect, ToColor(color));
    }

    public void Panel(Vector2 min, Vector2 max, uint background, uint border, float rounding, float thickness)
    {
        var rect = ToRect(min, max);
        rounding = MathF.Min(rounding, MathF.Min(rect.Width, rect.Height) * 0.5f);
        StyleBoxFlat.FillRounded(Renderer, rect, rounding, ToColor(border));
        StyleBoxFlat.FillRounded(Renderer, rect.Shrink(thickness, thickness, thickness, thickness), MathF.Max(0f, rounding - thickness), ToColor(background));
    }

    public void Rect(Vector2 min, Vector2 max, uint color, float thickness)
    {
        CanvasDraw.Outline(Renderer, ToRect(min, max), ToColor(color), thickness, thickness, thickness, thickness);
    }

    public void Line(Vector2 from, Vector2 to, uint color, float thickness)
    {
        CanvasShapes.Line(Renderer, new GlVector2(from.X, from.Y), new GlVector2(to.X, to.Y), ToColor(color), thickness);
    }

    public void Diamond(Vector2 center, float radius, uint color)
    {
        var c = ToColor(color);
        int rows = Math.Max(2, (int)MathF.Ceiling(radius * 2f));
        float rowHeight = radius * 2f / rows;
        for (int i = 0; i < rows; i++)
        {
            float y = -radius + rowHeight * (i + 0.5f);
            float half = radius - MathF.Abs(y);
            Renderer.DrawRect(new Rect2(center.X - half, center.Y - radius + rowHeight * i, half * 2f, rowHeight), c);
        }
    }

    public void Sprite(AtlasSprite sprite, Vector2 min, float size, float alpha)
    {
        AtlasSpriteDraw.Draw(Renderer, sprite, new Rect2(min.X, min.Y, size, size), new Color4(1f, 1f, 1f, alpha));
    }

    public static Color4 ToColor(uint color)
    {
        return new Color4((color & 0xFF) / 255f, ((color >> 8) & 0xFF) / 255f, ((color >> 16) & 0xFF) / 255f, ((color >> 24) & 0xFF) / 255f);
    }

    private static Rect2 ToRect(Vector2 min, Vector2 max) => new Rect2(min.X, min.Y, max.X - min.X, max.Y - min.Y);
}
