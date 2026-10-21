using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class CanvasCommandList : ICanvasRenderer
{
    public readonly struct Command
    {
        public readonly Rect2 Rect;
        public readonly Color4 Color;
        public readonly Texture2D Texture;

        public Command(Rect2 rect, Color4 color, Texture2D texture)
        {
            Rect = rect;
            Color = color;
            Texture = texture;
        }
    }

    private readonly Stack<Rect2> _clips = new Stack<Rect2>();

    public Transform2D Transform { get; set; } = Transform2D.Identity;
    public Color4 Modulate { get; set; } = Color4.White;
    public List<Command> Commands { get; } = new List<Command>();
    public int MaxClipDepth { get; private set; }

    public void DrawRect(Rect2 rect, Color4 color)
    {
        Commands.Add(new Command(Transform.TransformAxisAligned(rect), Multiply(color), null));
    }

    public void DrawTexture(Texture2D texture, Rect2 rect, Rect2 uv, Color4 color)
    {
        Commands.Add(new Command(Transform.TransformAxisAligned(rect), Multiply(color), texture));
    }

    public void PushClip(Rect2 rect)
    {
        _clips.Push(Transform.TransformAxisAligned(rect));
        MaxClipDepth = Math.Max(MaxClipDepth, _clips.Count);
    }

    public void PopClip()
    {
        if (_clips.Count > 0) _clips.Pop();
    }

    private Color4 Multiply(Color4 color)
    {
        return new Color4(color.R * Modulate.R, color.G * Modulate.G, color.B * Modulate.B, color.A * Modulate.A);
    }
}
