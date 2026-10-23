using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public static class CanvasShapes
{
    public const int DefaultCircleSegments = 24;

    public static void Line(ICanvasRenderer renderer, Vector2 from, Vector2 to, Color4 color, float thickness = 1f)
    {
        if (color.A <= 0f || thickness <= 0f) return;
        var delta = to - from;
        float length = delta.Length;
        if (length < 0.001f) return;

        var previous = renderer.Transform;
        renderer.Transform = previous * Transform2D.Create(from, MathF.Atan2(delta.Y, delta.X), Vector2.One);
        renderer.DrawRect(new Rect2(0f, -thickness * 0.5f, length, thickness), color);
        renderer.Transform = previous;
    }

    public static void FilledCircle(ICanvasRenderer renderer, Vector2 center, float radius, Color4 color)
    {
        if (color.A <= 0f || radius <= 0f) return;
        int rows = CircleRows(radius);
        float rowHeight = radius * 2f / rows;
        for (int i = 0; i < rows; i++)
        {
            float y = -radius + rowHeight * (i + 0.5f);
            float halfWidth = MathF.Sqrt(MathF.Max(0f, radius * radius - y * y));
            renderer.DrawRect(new Rect2(center.X - halfWidth, center.Y - radius + rowHeight * i, halfWidth * 2f, rowHeight), color);
        }
    }

    public static void Circle(ICanvasRenderer renderer, Vector2 center, float radius, Color4 color, float thickness = 1f, int segments = DefaultCircleSegments)
    {
        if (color.A <= 0f || radius <= 0f || segments < 3) return;
        var previous = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * MathF.Tau / segments;
            var point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Line(renderer, previous, point, color, thickness);
            previous = point;
        }
    }

    public static int CircleRows(float radius)
    {
        return Math.Clamp((int)MathF.Ceiling(radius), 2, 32);
    }
}
