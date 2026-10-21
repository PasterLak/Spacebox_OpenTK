using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class Sprite2D : Node2D
{
    public Texture2D Texture { get; set; }
    public Rect2? Region { get; set; }
    public bool Centered { get; set; } = true;
    public Vector2 Offset { get; set; }
    public bool FlipH { get; set; }
    public bool FlipV { get; set; }

    public Vector2 TextureSize => Region?.Size ?? (Texture == null ? Vector2.Zero : new Vector2(Texture.Width, Texture.Height));

    protected override void Draw(ICanvasRenderer renderer)
    {
        if (Texture == null) return;
        var size = TextureSize;
        var position = Centered ? Offset - size * 0.5f : Offset;
        renderer.DrawTexture(Texture, new Rect2(position, size), CanvasDraw.UvOf(Texture, Region, FlipH, FlipV), Color4.White);
    }
}
