using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public readonly struct Transform2D
{
    public static readonly Transform2D Identity = new Transform2D(Vector2.UnitX, Vector2.UnitY, Vector2.Zero);

    public readonly Vector2 X;
    public readonly Vector2 Y;
    public readonly Vector2 Origin;

    public Transform2D(Vector2 x, Vector2 y, Vector2 origin)
    {
        X = x;
        Y = y;
        Origin = origin;
    }

    public static Transform2D Translation(Vector2 offset) => new Transform2D(Vector2.UnitX, Vector2.UnitY, offset);

    public static Transform2D Scaling(Vector2 scale) => new Transform2D(new Vector2(scale.X, 0f), new Vector2(0f, scale.Y), Vector2.Zero);

    public static Transform2D Create(Vector2 position, float rotation, Vector2 scale)
    {
        float c = MathF.Cos(rotation);
        float s = MathF.Sin(rotation);
        return new Transform2D(new Vector2(c, s) * scale.X, new Vector2(-s, c) * scale.Y, position);
    }

    public bool IsAxisAligned => X.Y == 0f && Y.X == 0f;

    public Vector2 Apply(Vector2 point) => X * point.X + Y * point.Y + Origin;

    public Vector2 ApplyVector(Vector2 vector) => X * vector.X + Y * vector.Y;

    public Transform2D Inverse()
    {
        float det = X.X * Y.Y - Y.X * X.Y;
        if (MathF.Abs(det) < 1e-12f) return Identity;
        float inv = 1f / det;
        var ix = new Vector2(Y.Y, -X.Y) * inv;
        var iy = new Vector2(-Y.X, X.X) * inv;
        var io = -(ix * Origin.X + iy * Origin.Y);
        return new Transform2D(ix, iy, io);
    }

    public static Transform2D operator *(Transform2D parent, Transform2D child)
    {
        return new Transform2D(parent.ApplyVector(child.X), parent.ApplyVector(child.Y), parent.Apply(child.Origin));
    }

    public Rect2 TransformAxisAligned(Rect2 rect)
    {
        var a = Apply(rect.Position);
        var b = Apply(rect.End);
        var min = Vector2.ComponentMin(a, b);
        var max = Vector2.ComponentMax(a, b);
        return new Rect2(min, max - min);
    }
}
