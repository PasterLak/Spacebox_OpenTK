using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class ItemControlsControl : HudOverlayControl
{
    private const float Margin = 20f;

    private readonly ItemControlsUI _itemControls;

    public ItemControlsControl(ItemControlsUI itemControls)
    {
        _itemControls = itemControls;
    }

    protected override bool IsShown() => base.IsShown() && _itemControls.IsVisible && !string.IsNullOrEmpty(_itemControls.Text);

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        var lines = _itemControls.Text.Split('\n');
        float lineHeight = LineHeight;
        float y = LocalRect.Height - Margin - lines.Length * lineHeight;
        var color = _itemControls.TextColor;
        for (int i = 0; i < lines.Length; i++) DrawText(renderer, new Vector2(Margin, y + i * lineHeight), lines[i], color);
    }
}
