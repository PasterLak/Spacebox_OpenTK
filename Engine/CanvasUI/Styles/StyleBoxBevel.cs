using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class StyleBoxBevel : StyleBox
{
    public StyleBoxBevel(Color4 background, Color4 light, Color4 dark, float width = 2f, bool sunken = false)
    {
        BgColor = background;
        LightColor = light;
        DarkColor = dark;
        Width = width;
        Sunken = sunken;
    }

    public Color4 BgColor { get; set; }
    public Color4 LightColor { get; set; }
    public Color4 DarkColor { get; set; }
    public float Width { get; set; }
    public bool Sunken { get; set; }

    public override void Draw(ICanvasRenderer renderer, Rect2 rect)
    {
        if (BgColor.A > 0f) renderer.DrawRect(rect, BgColor);
        if (Width <= 0f) return;

        var topLeft = Sunken ? DarkColor : LightColor;
        var bottomRight = Sunken ? LightColor : DarkColor;
        renderer.DrawRect(new Rect2(rect.X, rect.Y, rect.Width, Width), topLeft);
        renderer.DrawRect(new Rect2(rect.X, rect.Y, Width, rect.Height), topLeft);
        renderer.DrawRect(new Rect2(rect.X, rect.End.Y - Width, rect.Width, Width), bottomRight);
        renderer.DrawRect(new Rect2(rect.End.X - Width, rect.Y, Width, rect.Height), bottomRight);
    }
}
