using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ColorSwatch : Control
{
    public ColorSwatch(float size)
    {
        MouseFilter = MouseFilter.Ignore;
        CustomMinimumSize = new Vector2(size, size);
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
    }

    public Color4 Color { get; set; } = Color4.White;

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, new Color4(0f, 0f, 0f, 0.6f));
        renderer.DrawRect(new Rect2(1f, 1f, Rect.Width - 2f, Rect.Height - 2f), Color);
    }
}
