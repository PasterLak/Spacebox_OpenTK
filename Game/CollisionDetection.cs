using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public static class CollisionDetection
    {
        public static bool SphereIntersectsAABB(BoundingSphere sphere, BoundingBox box)
        {
           
            Vector3 closestPoint = Vector3.Clamp(sphere.Center, box.Min, box.Max);

            float distanceSquared = (sphere.Center - closestPoint).LengthSquared;

            return distanceSquared < (sphere.Radius * sphere.Radius);
        }
    }
}
