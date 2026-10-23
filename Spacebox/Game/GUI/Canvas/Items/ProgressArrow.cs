using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class ProgressArrow : Control
{
    private const float HeadShare = 0.38f;
    private const float ShaftShare = 0.42f;
    private static readonly Color4 Background = new Color4(0.1f, 0.1f, 0.12f, 1f);
    private static readonly Color4 Fill = new Color4(0.95f, 0.55f, 0.15f, 1f);

    public ProgressArrow(Vector2 size)
    {
        CustomMinimumSize = size;
        MouseFilter = MouseFilter.Ignore;
    }

    public float Progress { get; set; }

    protected override void Draw(ICanvasRenderer renderer)
    {
        DrawShape(renderer, Background, Size.X);
        float filled = MathF.Round(Size.X * Math.Clamp(Progress, 0f, 1f));
        if (filled > 0f) DrawShape(renderer, Fill, filled);
    }

    private void DrawShape(ICanvasRenderer renderer, Color4 color, float limit)
    {
        float headX = MathF.Round(Size.X * (1f - HeadShare));
        float shaft = MathF.Round(Size.Y * ShaftShare);
        float shaftTop = MathF.Round((Size.Y - shaft) * 0.5f);
        renderer.DrawRect(new Rect2(0f, shaftTop, MathF.Min(headX, limit), shaft), color);

        int rows = (int)Size.Y;
        float headWidth = Size.X - headX;
        float half = Size.Y * 0.5f;
        for (int y = 0; y < rows; y++)
        {
            float distance = MathF.Abs(y + 0.5f - half) / half;
            float width = MathF.Round(headWidth * (1f - distance));
            float right = MathF.Min(headX + width, limit);
            if (right > headX) renderer.DrawRect(new Rect2(headX, y, right - headX, 1f), color);
        }
    }
}
