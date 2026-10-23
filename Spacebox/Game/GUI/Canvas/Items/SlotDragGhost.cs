using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class SlotDragGhost : ScreenOverlay
{
    private const float Size = 36f;

    protected override void Draw(ICanvasRenderer renderer)
    {
        var slot = SlotDrag.Source;
        if (slot == null || !slot.HasItem || Root == null) return;

        var pointer = Root.PointerPosition - GlobalPosition;
        var rect = new Rect2(MathF.Round(pointer.X - Size * 0.5f), MathF.Round(pointer.Y - Size * 0.5f), Size, Size);
        AtlasSpriteDraw.Draw(renderer, slot.Item.Icon, rect, Color4.White);
        if (slot.Count <= 1) return;

        var font = GetThemeFont();
        if (font == null) return;
        string text = slot.Count.ToString();
        float size = GetThemeFontSize();
        var textSize = font.Measure(text, size);
        var position = new Vector2(rect.X + rect.Width - textSize.X, rect.Y + rect.Height - textSize.Y);
        CanvasDraw.Text(renderer, font, position + Vector2.One, text, size, GetThemeColor("font_shadow_color", Color4.Black));
        CanvasDraw.Text(renderer, font, position, text, size, Color4.White);
    }
}
