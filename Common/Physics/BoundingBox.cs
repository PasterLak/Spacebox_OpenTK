using OpenTK.Mathematics;


namespace Spacebox.Common.Physics
{

    public abstract class BoundingVolume
    {
        public Vector3 Center { get; set; }
        public abstract bool Intersects(BoundingVolume other);
        public abstract float GetLongestSide();
        public abstract BoundingVolume Clone();
    }

    public class BoundingBox : BoundingVolume
    {

        public Vector3 Size { get; set; }

        public Vector3 Extents => Size * 0.5f;
        public Vector3 Min => Center - Extents;
        public Vector3 Max => Center + Extents;

        public BoundingBox(Vector3 center, Vector3 size)
        {
            Center = center;
            Size = size;
        }

        public static BoundingBox CreateFromMinMax(Vector3 min, Vector3 max)
        {
            var center = (min + max) * 0.5f;
            var size = max - min;
            return new BoundingBox(center, size);
        }


        public override BoundingVolume Clone()
        {
            return new BoundingBox(Center, Size);
        }

        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public override bool Intersects(BoundingVolume other)
        {
            if (other is BoundingBox box)
            {
                return Min.X <= box.Max.X && Max.X >= box.Min.X &&
                       Min.Y <= box.Max.Y && Max.Y >= box.Min.Y &&
                       Min.Z <= box.Max.Z && Max.Z >= box.Min.Z;
            }
            else if (other is BoundingSphere sphere)
            {
                var closestPoint = Vector3.Clamp(sphere.Center, Min, Max);
                var distanceSquared = (sphere.Center - closestPoint).LengthSquared;
                return distanceSquared <= sphere.Radius * sphere.Radius;
            }

            return false;
        }

        public override string ToString()
        {
            return $"Box center: {Center}, size: {Size}";
        }

        public override float GetLongestSide()
        {

            return Extents.X;
        }
    }

    

    




}
