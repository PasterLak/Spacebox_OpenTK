using OpenTK.Mathematics;

namespace Spacebox.Game.Physics;

public static class SphereBoxMath
{
    private const float MinDistanceSquared = 1e-10f;

    public static bool TryGetContact(Vector3 center, float radius, Vector3 min, Vector3 max, out Vector3 normal, out float depth)
    {
        var closest = Vector3.Clamp(center, min, max);
        var delta = center - closest;
        float distanceSquared = delta.LengthSquared;

        if (distanceSquared >= radius * radius)
        {
            normal = Vector3.Zero;
            depth = 0f;
            return false;
        }

        if (distanceSquared > MinDistanceSquared)
        {
            float distance = MathF.Sqrt(distanceSquared);
            normal = delta / distance;
            depth = radius - distance;
            return true;
        }

        GetNearestFace(center, min, max, out normal, out float faceDistance);
        depth = radius + faceDistance;
        return true;
    }

    public static Vector3 RemoveInward(Vector3 vector, Vector3 normal, out float inwardSpeed)
    {
        float dot = Vector3.Dot(vector, normal);
        if (dot >= 0f)
        {
            inwardSpeed = 0f;
            return vector;
        }

        inwardSpeed = -dot;
        return vector - normal * dot;
    }

    private static void GetNearestFace(Vector3 point, Vector3 min, Vector3 max, out Vector3 normal, out float distance)
    {
        distance = point.X - min.X;
        normal = -Vector3.UnitX;

        Check(max.X - point.X, Vector3.UnitX, ref normal, ref distance);
        Check(point.Y - min.Y, -Vector3.UnitY, ref normal, ref distance);
        Check(max.Y - point.Y, Vector3.UnitY, ref normal, ref distance);
        Check(point.Z - min.Z, -Vector3.UnitZ, ref normal, ref distance);
        Check(max.Z - point.Z, Vector3.UnitZ, ref normal, ref distance);
    }

    private static void Check(float candidateDistance, Vector3 candidateNormal, ref Vector3 normal, ref float distance)
    {
        if (candidateDistance >= distance) return;
        distance = candidateDistance;
        normal = candidateNormal;
    }
}
