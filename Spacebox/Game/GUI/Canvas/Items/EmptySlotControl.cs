using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class EmptySlotControl : Control
{
    public EmptySlotControl(float size)
    {
        ThemeTypeVariation = "Slot";
        MouseFilter = MouseFilter.Ignore;
        CustomMinimumSize = new Vector2(size, size);
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        GetThemeStyleBox("normal")?.Draw(renderer, LocalRect);
    }
}
