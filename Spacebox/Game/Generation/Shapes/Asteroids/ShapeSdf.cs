using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes.Asteroids;

public static class ShapeSdf
{
    public static float Sphere(Vector3 p, Vector3 center, float radius) => (p - center).Length - radius;

    public static float Ellipsoid(Vector3 p, Vector3 center, Vector3 radii)
    {
        var d = p - center;
        float k0 = new Vector3(d.X / radii.X, d.Y / radii.Y, d.Z / radii.Z).Length;
        if (k0 < 1e-5f) return -MathF.Min(radii.X, MathF.Min(radii.Y, radii.Z));

        float k1 = new Vector3(d.X / (radii.X * radii.X), d.Y / (radii.Y * radii.Y), d.Z / (radii.Z * radii.Z)).Length;
        return k0 * (k0 - 1f) / k1;
    }

    public static float Torus(Vector3 p, float major, float tube)
    {
        float ring = MathF.Sqrt(p.X * p.X + p.Z * p.Z) - major;
        return MathF.Sqrt(ring * ring + p.Y * p.Y) - tube;
    }

    public static float Capsule(Vector3 p, Vector3 a, Vector3 b, float radius)
    {
        var pa = p - a;
        var ba = b - a;
        float h = Math.Clamp(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba), 0f, 1f);
        return (pa - ba * h).Length - radius;
    }

    public static float Cone(Vector3 p, Vector3 axis, float halfAngle)
    {
        float length = p.Length;
        if (length < 1e-5f) return 0f;

        float angle = MathF.Acos(Math.Clamp(Vector3.Dot(p / length, axis), -1f, 1f));
        float delta = angle - halfAngle;
        return delta >= MathF.PI * 0.5f ? length : length * MathF.Sin(delta);
    }

    public static float SmoothUnion(float a, float b, float k)
    {
        if (k <= 0f) return MathF.Min(a, b);
        float h = Math.Clamp(0.5f + 0.5f * (b - a) / k, 0f, 1f);
        return MathHelper.Lerp(b, a, h) - k * h * (1f - h);
    }

    public static float SmoothSubtract(float shape, float cutter, float k)
    {
        if (k <= 0f) return MathF.Max(shape, -cutter);
        float h = Math.Clamp(0.5f - 0.5f * (shape + cutter) / k, 0f, 1f);
        return MathHelper.Lerp(shape, -cutter, h) + k * h * (1f - h);
    }
}
