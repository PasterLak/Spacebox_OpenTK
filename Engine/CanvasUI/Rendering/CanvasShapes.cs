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

    public static void RectOutline(ICanvasRenderer renderer, Rect2 rect, Color4 color, float thickness = 1f)
    {
        if (color.A <= 0f || thickness <= 0f) return;
        CanvasDraw.Outline(renderer, rect, color, thickness, thickness, thickness, thickness);
    }

    public static void FilledTriangle(ICanvasRenderer renderer, Vector2 a, Vector2 b, Vector2 c, Color4 color)
    {
        if (color.A <= 0f) return;
        float top = MathF.Min(a.Y, MathF.Min(b.Y, c.Y));
        float bottom = MathF.Max(a.Y, MathF.Max(b.Y, c.Y));
        float height = bottom - top;
        if (height < 0.001f) return;

        int rows = TriangleRows(height);
        float rowHeight = height / rows;
        for (int i = 0; i < rows; i++)
        {
            float y = top + rowHeight * (i + 0.5f);
            float min = float.MaxValue;
            float max = float.MinValue;
            Cross(a, b, y, ref min, ref max);
            Cross(b, c, y, ref min, ref max);
            Cross(c, a, y, ref min, ref max);
            if (max <= min) continue;
            renderer.DrawRect(new Rect2(min, top + rowHeight * i, max - min, rowHeight), color);
        }
    }

    public static void FilledSector(ICanvasRenderer renderer, Vector2 center, float radius, float startAngle, float endAngle, Color4 color, int segments = 8)
    {
        if (color.A <= 0f || radius <= 0f || segments < 1) return;
        float step = (endAngle - startAngle) / segments;
        var previous = center + Direction(startAngle) * radius;
        for (int i = 1; i <= segments; i++)
        {
            var point = center + Direction(startAngle + step * i) * radius;
            FilledTriangle(renderer, center, previous, point, color);
            previous = point;
        }
    }

    public static void Arc(ICanvasRenderer renderer, Vector2 center, float radius, float startAngle, float endAngle, Color4 color, float thickness = 1f, int segments = 8)
    {
        if (color.A <= 0f || radius <= 0f || segments < 1) return;
        float step = (endAngle - startAngle) / segments;
        var previous = center + Direction(startAngle) * radius;
        for (int i = 1; i <= segments; i++)
        {
            var point = center + Direction(startAngle + step * i) * radius;
            Line(renderer, previous, point, color, thickness);
            previous = point;
        }
    }

    public static int TriangleRows(float height)
    {
        return Math.Clamp((int)MathF.Ceiling(height), 2, 48);
    }

    private static Vector2 Direction(float angle) => new Vector2(MathF.Cos(angle), MathF.Sin(angle));

    private static void Cross(Vector2 from, Vector2 to, float y, ref float min, ref float max)
    {
        if ((y < from.Y && y < to.Y) || (y > from.Y && y > to.Y) || MathF.Abs(to.Y - from.Y) < 0.0001f) return;
        float x = from.X + (y - from.Y) / (to.Y - from.Y) * (to.X - from.X);
        if (x < min) min = x;
        if (x > max) max = x;
    }
}
