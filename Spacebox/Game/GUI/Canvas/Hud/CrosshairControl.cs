using Engine.CanvasUI;
using Spacebox.GUI;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class CrosshairControl : HudOverlayControl
{
    private const string Cross = "+";

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        var center = Center;
        var color = CenteredText.Color;
        if (!CenteredText.IsVisible)
        {
            DrawText(renderer, center - Measure(Cross) * 0.5f, Cross, color);
            return;
        }

        var lines = CenteredText.Text.Split('\n');
        float lineHeight = LineHeight;
        float y = center.Y - lineHeight * 0.5f;
        for (int i = 0; i < lines.Length; i++) DrawCenteredText(renderer, center.X, y + i * lineHeight, lines[i], color);
    }
}
