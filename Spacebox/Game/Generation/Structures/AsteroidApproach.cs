using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;

public readonly record struct AsteroidTarget(Vector3 Center, float Radius);

public static class AsteroidApproach
{
    public const float SurfaceMargin = 8f;
    public const float AlreadyThereDistance = 24f;

    public static float SurfaceDistance(AsteroidTarget target, Vector3 position)
    {
        return MathF.Max(0f, (position - target.Center).Length - target.Radius);
    }

    public static bool IsBetter(AsteroidTarget candidate, Vector3 position, float bestDistance, out float distance)
    {
        distance = SurfaceDistance(candidate, position);
        return distance >= AlreadyThereDistance && distance < bestDistance;
    }

    public static Vector3 ApproachPoint(AsteroidTarget target, Vector3 from)
    {
        var offset = from - target.Center;
        var direction = offset.LengthSquared > 0.0001f ? offset.Normalized() : Vector3.UnitY;
        return target.Center + direction * (target.Radius + SurfaceMargin);
    }
}
