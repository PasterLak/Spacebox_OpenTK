using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public readonly struct Rect2 : IEquatable<Rect2>
{
    public readonly Vector2 Position;
    public readonly Vector2 Size;

    public Rect2(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public Rect2(float x, float y, float width, float height) : this(new Vector2(x, y), new Vector2(width, height))
    {
    }

    public float X => Position.X;
    public float Y => Position.Y;
    public float Width => Size.X;
    public float Height => Size.Y;
    public Vector2 End => Position + Size;
    public Vector2 Center => Position + Size * 0.5f;
    public bool HasArea => Size.X > 0f && Size.Y > 0f;
    public float Area => MathF.Max(0f, Size.X) * MathF.Max(0f, Size.Y);

    public bool Contains(Vector2 point)
    {
        return point.X >= Position.X && point.Y >= Position.Y && point.X < Position.X + Size.X && point.Y < Position.Y + Size.Y;
    }

    public bool Encloses(Rect2 other, float tolerance = 0f)
    {
        return other.X >= X - tolerance && other.Y >= Y - tolerance && other.End.X <= End.X + tolerance && other.End.Y <= End.Y + tolerance;
    }

    public bool Intersects(Rect2 other, float tolerance = 0f)
    {
        return other.X < End.X - tolerance && other.End.X > X + tolerance && other.Y < End.Y - tolerance && other.End.Y > Y + tolerance;
    }

    public Rect2 Intersection(Rect2 other)
    {
        float x0 = MathF.Max(X, other.X);
        float y0 = MathF.Max(Y, other.Y);
        float x1 = MathF.Min(End.X, other.End.X);
        float y1 = MathF.Min(End.Y, other.End.Y);
        return new Rect2(x0, y0, MathF.Max(0f, x1 - x0), MathF.Max(0f, y1 - y0));
    }

    public Rect2 Grow(float left, float top, float right, float bottom)
    {
        return new Rect2(X - left, Y - top, Width + left + right, Height + top + bottom);
    }

    public Rect2 Shrink(float left, float top, float right, float bottom)
    {
        return Grow(-left, -top, -right, -bottom);
    }

    public bool Equals(Rect2 other) => Position == other.Position && Size == other.Size;
    public override bool Equals(object obj) => obj is Rect2 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Position, Size);
    public static bool operator ==(Rect2 a, Rect2 b) => a.Equals(b);
    public static bool operator !=(Rect2 a, Rect2 b) => !a.Equals(b);
    public override string ToString() => $"[{X}, {Y}, {Width}x{Height}]";
}
