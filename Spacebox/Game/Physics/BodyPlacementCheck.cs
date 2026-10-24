using OpenTK.Mathematics;

namespace Spacebox.Game.Physics;

public static class BodyPlacementCheck
{
    public const float RadiusFactor = 0.9f;

    public static bool SphereIntersectsBox(Vector3 boxMin, Vector3 boxMax, Vector3 center, float radius)
    {
        float dx = MathF.Max(boxMin.X - center.X, MathF.Max(0f, center.X - boxMax.X));
        float dy = MathF.Max(boxMin.Y - center.Y, MathF.Max(0f, center.Y - boxMax.Y));
        float dz = MathF.Max(boxMin.Z - center.Z, MathF.Max(0f, center.Z - boxMax.Z));
        return dx * dx + dy * dy + dz * dz < radius * radius;
    }

    public static bool AnyBodyInCell(IReadOnlyList<SphereBody> bodies, Vector3 frameOrigin, Quaternion inverseRotation, Vector3 cellMin)
    {
        if (bodies == null) return false;

        Vector3 cellMax = cellMin + Vector3.One;
        for (int i = 0; i < bodies.Count; i++)
        {
            var body = bodies[i];
            if (body == null || !body.Enabled || !body.IsSolid) continue;

            Vector3 local = Vector3.Transform(body.Center - frameOrigin, inverseRotation);
            if (SphereIntersectsBox(cellMin, cellMax, local, body.Radius * RadiusFactor)) return true;
        }
        return false;
    }
}
