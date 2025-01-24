using OpenTK.Mathematics;

namespace Spacebox.Common.Physics
{
    public struct Bounds
    {
        public Vector3 Center;
        public Vector3 Extents;

        public Bounds(Vector3 center, Vector3 size)
        {
            Center = center;
            Extents = size / 2f;
        }

        public Vector3 Size
        {
            get => Extents * 2f;
            set => Extents = value / 2f;
        }

        public Vector3 Min => Center - Extents;

        public Vector3 Max => Center + Extents;

        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public bool Contains(Bounds bounds)
        {
            return Contains(bounds.Min) && Contains(bounds.Max);
        }

        public bool Intersects(Bounds bounds)
        {
            return Min.X <= bounds.Max.X && Max.X >= bounds.Min.X &&
                   Min.Y <= bounds.Max.Y && Max.Y >= bounds.Min.Y &&
                   Min.Z <= bounds.Max.Z && Max.Z >= bounds.Min.Z;
        }

        public bool IntersectRay(Ray ray, out float distance)
        {
            // AABB 
            distance = 0f;
            float tmin = (Min.X - ray.Origin.X) / ray.Direction.X;
            float tmax = (Max.X - ray.Origin.X) / ray.Direction.X;

            if (tmin > tmax) Swap(ref tmin, ref tmax);

            float tymin = (Min.Y - ray.Origin.Y) / ray.Direction.Y;
            float tymax = (Max.Y - ray.Origin.Y) / ray.Direction.Y;

            if (tymin > tymax) Swap(ref tymin, ref tymax);

            if (tmin > tymax || tymin > tmax)
            {
                return false;
            }

            if (tymin > tmin)
                tmin = tymin;
            if (tymax < tmax)
                tmax = tymax;

            float tzmin = (Min.Z - ray.Origin.Z) / ray.Direction.Z;
            float tzmax = (Max.Z - ray.Origin.Z) / ray.Direction.Z;

            if (tzmin > tzmax) Swap(ref tzmin, ref tzmax);

            if (tmin > tzmax || tzmin > tmax)
            {
                return false;
            }

            if (tzmin > tmin)
                tmin = tzmin;
            if (tzmax < tmax)
                tmax = tzmax;

            distance = tmin;

            if (distance < 0)
            {
                distance = tmax;
                if (distance < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void Swap(ref float a, ref float b)
        {
            float temp = a;
            a = b;
            b = temp;
        }
    }


}
