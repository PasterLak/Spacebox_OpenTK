using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public abstract class HudOverlayControl : ScreenOverlay
{
    protected float Unit => LocalRect.Height / 100f;
    protected Vector2 Center => new Vector2(LocalRect.Width * 0.5f, LocalRect.Height * 0.5f);
    protected Color4 TextColor => GetThemeColor("hud_text_color", Color4.White);
    protected Color4 ShadowColor => GetThemeColor("font_shadow_color", Color4.Black);

    protected sealed override void Draw(ICanvasRenderer renderer)
    {
        if (!IsShown()) return;
        DrawOverlay(renderer);
    }

    protected virtual bool IsShown() => Settings.ShowInterface;

    protected abstract void DrawOverlay(ICanvasRenderer renderer);

    protected Vector2 Measure(string text)
    {
        var font = GetThemeFont();
        return font == null || string.IsNullOrEmpty(text) ? Vector2.Zero : font.Measure(text, GetThemeFontSize());
    }

    protected float LineHeight
    {
        get
        {
            var font = GetThemeFont();
            return font == null ? 12f : font.GetLineHeight(GetThemeFontSize());
        }
    }

    protected void DrawText(ICanvasRenderer renderer, Vector2 position, string text, Color4 color)
    {
        var font = GetThemeFont();
        if (font == null || string.IsNullOrEmpty(text)) return;
        float size = GetThemeFontSize();
        var rounded = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));
        CanvasDraw.Text(renderer, font, rounded + Vector2.One, text, size, new Color4(ShadowColor.R, ShadowColor.G, ShadowColor.B, ShadowColor.A * color.A));
        CanvasDraw.Text(renderer, font, rounded, text, size, color);
    }

    protected void DrawCenteredText(ICanvasRenderer renderer, float centerX, float y, string text, Color4 color)
    {
        DrawText(renderer, new Vector2(centerX - Measure(text).X * 0.5f, y), text, color);
    }

    protected static void DrawLine(ICanvasRenderer renderer, Vector2 from, Vector2 to, float thickness, Color4 color)
    {
        var delta = to - from;
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(delta.X), MathF.Abs(delta.Y)) / MathF.Max(1f, thickness * 0.5f)));
        for (int i = 0; i <= steps; i++)
        {
            var point = from + delta * (i / (float)steps);
            renderer.DrawRect(new Rect2(MathF.Round(point.X - thickness * 0.5f), MathF.Round(point.Y - thickness * 0.5f), thickness, thickness), color);
        }
    }
}
