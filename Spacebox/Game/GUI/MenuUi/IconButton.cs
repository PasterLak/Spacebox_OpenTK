using Engine;
using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI.Canvas;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class IconButton : Control
{
    private Texture2D _texture;
    private AtlasSprite _sprite;

    public IconButton(float size)
    {
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilter.Stop;
    }

    public bool FlipV { get; set; }
    public event Action Clicked;
    public override bool IsInteractive => true;

    public void SetTexture(Texture2D texture)
    {
        _texture = texture;
        _sprite = default;
    }

    public void SetSprite(AtlasSprite sprite)
    {
        _sprite = sprite;
        _texture = null;
    }

    private static Rect2 Fit(Rect2 rect, float width, float height)
    {
        if (width <= 0f || height <= 0f) return rect;
        float scale = MathF.Min(rect.Width / width, rect.Height / height);
        var size = new Vector2(width * scale, height * scale);
        return new Rect2(rect.Position + (rect.Size - size) * 0.5f, size);
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        Clicked?.Invoke();
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var tint = IsHovered ? Color4.White : new Color4(1f, 1f, 1f, 0.8f);
        var rect = LocalRect.Shrink(2f, 2f, 2f, 2f);
        if (_texture != null) renderer.DrawTexture(_texture, Fit(rect, _texture.Width, _texture.Height), CanvasDraw.UvOf(_texture, null, false, FlipV), tint);
        else AtlasSpriteDraw.Draw(renderer, _sprite, rect, tint);
    }
}
