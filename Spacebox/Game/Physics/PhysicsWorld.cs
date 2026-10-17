using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Physics;

public sealed class PhysicsWorld
{
    private readonly World _world;
    private readonly List<SphereBody> _bodies = new List<SphereBody>();
    private readonly BoundingSphere _probe = new BoundingSphere(Vector3.Zero, 0f);

    public PhysicsWorld(World world)
    {
        _world = world;
    }

    public void Register(SphereBody body)
    {
        if (!_bodies.Contains(body)) _bodies.Add(body);
    }

    public void Unregister(SphereBody body)
    {
        _bodies.Remove(body);
    }

    public bool IsBlocked(Vector3 center, float radius, SphereBody ignore)
    {
        return OverlapsVoxels(center, radius, out _) || OverlapsSolidBody(center, radius, ignore, out _);
    }

    public bool OverlapsVoxels(Vector3 center, float radius, out CollideInfo collideInfo)
    {
        _probe.Center = center;
        _probe.Radius = radius;
        return _world.IsColliding(center, _probe, out collideInfo);
    }

    public bool OverlapsSolidBody(Vector3 center, float radius, SphereBody ignore, out SphereBody other)
    {
        for (int i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i];
            if (body == ignore || !body.IsSolid || !body.Enabled) continue;

            float radiusSum = radius + body.Radius;
            if (Vector3.DistanceSquared(center, body.Center) < radiusSum * radiusSum)
            {
                other = body;
                return true;
            }
        }

        other = null;
        return false;
    }

    public bool Raycast(Ray ray, SphereBody ignore, out SphereRayHit hit)
    {
        SphereBody closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i];
            if (body == ignore || !body.Enabled) continue;

            if (ray.IntersectsSphere(body.Center, body.Radius, out float distance) && distance < closestDistance)
            {
                closestDistance = distance;
                closest = body;
            }
        }

        if (closest == null)
        {
            hit = default;
            return false;
        }

        hit = new SphereRayHit(closest, closestDistance, ray.GetPoint(closestDistance));
        return true;
    }
}
