using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public interface ICanvasRenderer
{
    Transform2D Transform { get; set; }
    Color4 Modulate { get; set; }

    void DrawRect(Rect2 rect, Color4 color);
    void DrawTexture(Texture2D texture, Rect2 rect, Rect2 uv, Color4 color);
    void DrawTextureHandle(int handle, Rect2 rect, Rect2 uv, Color4 color);
    void PushClip(Rect2 rect);
    void PopClip();
}
