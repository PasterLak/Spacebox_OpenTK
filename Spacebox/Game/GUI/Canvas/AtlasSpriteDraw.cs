using Engine;
using Engine.Graphics.Atlas;
using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas;

public static class AtlasSpriteDraw
{
    public static void Draw(ICanvasRenderer renderer, AtlasSprite sprite, Rect2 rect, Color4 color)
    {
        if (!sprite.IsValid) return;
        var texture = sprite.Atlas.Texture;
        if (texture == null) return;

        var region = sprite.Region;
        var uv = new Rect2(region.UvMin.X, region.UvMax.Y, region.UvMax.X - region.UvMin.X, region.UvMin.Y - region.UvMax.Y);
        renderer.DrawTexture(texture, rect, uv, color);
    }
}
