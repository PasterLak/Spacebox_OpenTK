using Engine;
using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;

namespace Spacebox.Editor.Canvas;

public static class CanvasIcons
{
    public static bool TryGet(AtlasSprite sprite, out Texture2D texture, out Rect2 region)
    {
        texture = null;
        region = default;
        if (!sprite.IsValid || sprite.Atlas.Texture == null) return false;

        texture = sprite.Atlas.Texture;
        float w = texture.Width;
        float h = texture.Height;
        var r = sprite.Region;
        region = new Rect2(r.UvMin.X * w, (1f - r.UvMax.Y) * h, (r.UvMax.X - r.UvMin.X) * w, (r.UvMax.Y - r.UvMin.Y) * h);
        return true;
    }

    public static ListItem WithIcon(this ListItem item, AtlasSprite sprite)
    {
        if (TryGet(sprite, out var texture, out var region))
        {
            item.Icon = texture;
            item.IconRegion = region;
        }
        return item;
    }

    public static TextureRect Image(AtlasSprite sprite, float size)
    {
        var rect = new TextureRect { CustomMinimumSize = new Vector2(size, size), StretchMode = TextureStretchMode.KeepAspectCentered, MouseFilter = MouseFilter.Ignore };
        Set(rect, sprite);
        return rect;
    }

    public static void Set(TextureRect rect, AtlasSprite sprite)
    {
        if (TryGet(sprite, out var texture, out var region))
        {
            rect.Texture = texture;
            rect.Region = region;
        }
        else
        {
            rect.Texture = null;
        }
    }
}
