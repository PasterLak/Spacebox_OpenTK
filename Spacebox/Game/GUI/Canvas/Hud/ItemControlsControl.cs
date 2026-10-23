using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class ItemControlsControl : HudOverlayControl
{
    private const float Margin = 20f;

    protected override bool IsShown() => base.IsShown() && ItemControlsUI.IsVisible && !string.IsNullOrEmpty(ItemControlsUI.Text);

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        var lines = ItemControlsUI.Text.Split('\n');
        float lineHeight = LineHeight;
        float y = LocalRect.Height - Margin - lines.Length * lineHeight;
        var color = ItemControlsUI.TextColor;
        for (int i = 0; i < lines.Length; i++) DrawText(renderer, new Vector2(Margin, y + i * lineHeight), lines[i], color);
    }
}
