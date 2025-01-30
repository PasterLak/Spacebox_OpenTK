using OpenTK.Mathematics;


namespace Spacebox.Common.Physics
{
    
    public class BoundingBox : BoundingVolume
    {
        public Vector3 Size { get; set; }

        public Vector3 Extents => Size * 0.5f;
        public Vector3 Min => Center - Extents;
        public Vector3 Max => Center + Extents;

        public float Diagonal => Vector3.Distance(Min,Max);
        public float DiagonalSquared => Vector3.DistanceSquared(Min, Max);

        public BoundingBox(BoundingBox boundingBox)
        {
            Center = boundingBox.Center;
            Size = boundingBox.Size;
        }

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

        public static BoundingBox CreateFromPoints(Vector3 point1, Vector3 point2)
        {
            Vector3 min = Vector3.ComponentMin(point1, point2);
            Vector3 max = Vector3.ComponentMax(point1, point2);
            return BoundingBox.CreateFromMinMax(min, max);
        }

        public override BoundingVolume Clone()
        {
            return new BoundingBox(Center, Size);
        }


        public void Expand(BoundingBox other)
        {
            Vector3 currentMin = this.Min;
            Vector3 currentMax = this.Max;

            Vector3 otherMin = other.Min;
            Vector3 otherMax = other.Max;

            Vector3 newMin = Vector3.ComponentMin(currentMin, otherMin);
            Vector3 newMax = Vector3.ComponentMax(currentMax, otherMax);

            this.Center = (newMin + newMax) * 0.5f;
            this.Size = newMax - newMin;
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
            return MathHelper.Max(MathHelper.Max(Extents.X, Extents.Y), Extents.Z);
        }

    }
}