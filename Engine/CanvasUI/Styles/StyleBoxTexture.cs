using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class StyleBoxTexture : StyleBox
{
    public Texture2D Texture { get; set; }
    public Rect2? Region { get; set; }
    public float PatchLeft { get; set; }
    public float PatchTop { get; set; }
    public float PatchRight { get; set; }
    public float PatchBottom { get; set; }
    public Color4 Modulate { get; set; } = Color4.White;
    public bool DrawCenter { get; set; } = true;

    public StyleBoxTexture()
    {
    }

    public StyleBoxTexture(Texture2D texture, float patch, float contentMargin)
    {
        Texture = texture;
        PatchLeft = PatchTop = PatchRight = PatchBottom = patch;
        SetContentMargins(contentMargin);
    }

    public override void Draw(ICanvasRenderer renderer, Rect2 rect)
    {
        CanvasDraw.NinePatch(renderer, Texture, rect, Region, PatchLeft, PatchTop, PatchRight, PatchBottom, Modulate, DrawCenter);
    }
}
