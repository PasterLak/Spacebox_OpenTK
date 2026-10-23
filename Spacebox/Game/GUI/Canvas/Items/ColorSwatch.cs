using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class ColorSwatch : Control
{
    private static readonly Color4 SelectedOutline = new Color4(0.85f, 0.5f, 0.05f, 1f);
    private static readonly Color4 Outline = new Color4(0f, 0f, 0f, 0.6f);

    public ColorSwatch(Color4 color, float size, string tooltip)
    {
        Color = color;
        TooltipText = tooltip;
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilter.Stop;
    }

    public Color4 Color { get; }
    public bool Selected { get; set; }
    public event Action Clicked;
    public override bool IsInteractive => true;

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        Clicked?.Invoke();
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, new Color4(Color.R, Color.G, Color.B, 1f));
        float width = Selected ? 2f : 1f;
        CanvasDraw.Outline(renderer, LocalRect, Selected ? SelectedOutline : Outline, width, width, width, width);
    }
}
