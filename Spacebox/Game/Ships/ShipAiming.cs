using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public static class ShipAiming
{
    public static Vector3 ClampToCone(Vector3 axis, Vector3 direction, float coneDegrees)
    {
        axis = SafeNormalize(axis, Vector3.UnitY);
        direction = SafeNormalize(direction, axis);

        float maxAngle = MathHelper.DegreesToRadians(coneDegrees);
        float angle = AngleBetween(axis, direction);
        if (angle <= maxAngle) return direction;

        return RotateTowards(axis, direction, maxAngle);
    }

    public static Vector3 RotateTowards(Vector3 from, Vector3 to, float maxRadians)
    {
        from = SafeNormalize(from, Vector3.UnitY);
        to = SafeNormalize(to, from);

        float angle = AngleBetween(from, to);
        if (angle <= maxRadians || angle < 1e-5f) return to;

        var rotationAxis = Vector3.Cross(from, to);
        if (rotationAxis.LengthSquared < 1e-10f)
            rotationAxis = Vector3.Cross(from, MathF.Abs(from.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX);

        var rotation = Quaternion.FromAxisAngle(rotationAxis.Normalized(), maxRadians);
        return Vector3.Transform(from, rotation).Normalized();
    }

    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        var f = SafeNormalize(forward, -Vector3.UnitZ);
        var right = Vector3.Cross(f, up);
        if (right.LengthSquared < 1e-8f) right = Vector3.Cross(f, MathF.Abs(f.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX);
        var u = Vector3.Cross(right.Normalized(), f);
        return Quaternion.Normalize(Matrix4.LookAt(Vector3.Zero, f, u).ExtractRotation().Inverted());
    }

    public static float AngleBetween(Vector3 a, Vector3 b)
    {
        return MathF.Acos(Math.Clamp(Vector3.Dot(a, b), -1f, 1f));
    }

    public static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
    {
        float length = value.Length;
        return length > 1e-6f ? value / length : fallback;
    }
}
