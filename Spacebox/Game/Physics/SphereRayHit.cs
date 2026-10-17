using OpenTK.Mathematics;

namespace Spacebox.Game.Physics;

public readonly struct SphereRayHit
{
    public readonly SphereBody Body;
    public readonly float Distance;
    public readonly Vector3 Point;

    public SphereRayHit(SphereBody body, float distance, Vector3 point)
    {
        Body = body;
        Distance = distance;
        Point = point;
    }
}
