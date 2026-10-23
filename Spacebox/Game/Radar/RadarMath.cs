using OpenTK.Mathematics;

namespace Spacebox.Game.Radar;

public static class RadarMath
{
    private const float TwoPi = MathF.PI * 2f;

    public static Vector2 Project(Vector3 offset, Vector3 right, Vector3 front)
    {
        var planar = new Vector2(Vector3.Dot(offset, right), -Vector3.Dot(offset, front));
        float length = planar.Length;
        return length < 0.0001f ? Vector2.Zero : planar / length;
    }

    public static float SweepAngleOf(Vector2 direction) => Wrap(MathF.Atan2(-direction.Y, -direction.X));

    public static Vector2 DirectionOf(float sweepAngle) => new Vector2(-MathF.Cos(sweepAngle), -MathF.Sin(sweepAngle));

    public static bool Crossed(float previous, float current, float angle)
    {
        previous = Wrap(previous);
        current = Wrap(current);
        angle = Wrap(angle);
        if (previous <= current) return angle > previous && angle <= current;
        return angle > previous || angle <= current;
    }

    public static float Radius(float distance, float range)
    {
        if (range <= 0f) return 0.1f;
        float t = Math.Clamp(distance / range, 0f, 1f);
        return 0.1f + 0.75f * t;
    }

    public static float Wrap(float angle)
    {
        angle %= TwoPi;
        return angle < 0f ? angle + TwoPi : angle;
    }
}
