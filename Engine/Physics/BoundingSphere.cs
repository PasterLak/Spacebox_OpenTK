using OpenTK.Mathematics;

namespace Engine.Physics
{
    public class BoundingSphere : BoundingVolume
    {

        public float Radius { get; set; }

        public BoundingSphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public override bool Intersects(BoundingVolume other)
        {
            if (other is BoundingSphere sphere)
            {
                float distanceSquared = (Center - sphere.Center).LengthSquared;
                float radiusSum = Radius + sphere.Radius;
                return distanceSquared <= radiusSum * radiusSum;
            }
            else if (other is BoundingBox box)
            {
                return box.Intersects(this);
            }

            return false;
        }

        public override BoundingVolume Clone()
        {
            return new BoundingSphere(Center, Radius);
        }

        public override string ToString()
        {
            return $"Sphere center: {Center}, radius: {Radius}";
        }

        public override float GetLongestSide()
        {
            return Radius;
        }
    }
}
