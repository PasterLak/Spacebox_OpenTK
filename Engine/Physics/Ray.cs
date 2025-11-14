using System.Runtime.CompilerServices;
using Engine.Components;
using OpenTK.Mathematics;

namespace Engine.Physics
{
    public class Ray
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; }
        public float Length { get; set; }

        public Ray(Vector3 origin, Vector3 direction, float length)
        {
            Origin = origin;
            Direction = direction.Normalized();
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetPoint(float distance) => Origin + Direction * distance;


        public bool Intersects(ColliderComponent colliderComponent, out float distance)
        {
            return Intersects(colliderComponent.Volume, out distance);
        }
        public bool Intersects(BoundingVolume volume, out float distance)
        {
            switch (volume)
            {
                case BoundingSphere sphere:
                    return Intersects(sphere, out distance);

                case BoundingBox aabb:
                    return Intersects(aabb, out distance);

                case BoundingBoxOBB obb:
                    return Intersects(obb, out distance);

                default:
                    distance = 0f;
                    return false;
            }
        }

        public bool Intersects(BoundingSphere sphere, out float distance)
        {
            var oc = Origin - sphere.Center;
            var b = Vector3.Dot(oc, Direction);
            var c = Vector3.Dot(oc, oc) - sphere.Radius * sphere.Radius;
            var h = b * b - c;
            if (h < 0)
            {
                distance = 0;
                return false;
            }
            h = MathF.Sqrt(h);
            var t = -b - h;
            if (t < 0) t = -b + h;
            if (t < 0 || t > Length)
            {
                distance = 0;
                return false;
            }
            distance = t;
            return true;
        }

        public bool Intersects(BoundingBox box, out float distance)
        {
            distance = 0f;
            float tMin = 0f;
            float tMax = Length;

            for (int i = 0; i < 3; i++)
            {
                float o = Origin[i];
                float d = Direction[i];
                float min = box.Min[i];
                float max = box.Max[i];

                if (MathF.Abs(d) < 1e-8f)
                {
                    if (o < min || o > max) return false;
                    continue;
                }

                float invD = 1f / d;
                float t1 = (min - o) * invD;
                float t2 = (max - o) * invD;
                if (t1 > t2)
                {
                    float tmp = t1;
                    t1 = t2;
                    t2 = tmp;
                }

                if (t1 > tMin) tMin = t1;
                if (t2 < tMax) tMax = t2;
                if (tMin > tMax) return false;
            }

            distance = tMin < 0 ? tMax : tMin;
            return distance >= 0 && distance <= Length;
        }

        public bool Intersects(BoundingBoxOBB obb, out float distance)
        {
            distance = 0f;
            var invRot = Quaternion.Invert(obb.Rotation);
            var localOrigin = Vector3.Transform(Origin - obb.Center, invRot);
            var localDir = Vector3.Transform(Direction, invRot);
            localDir.Normalize();

            var extents = obb.Extents;
            Vector3 min = -extents;
            Vector3 max = extents;

            float tMin = 0f;
            float tMax = Length;

            for (int i = 0; i < 3; i++)
            {
                float o = localOrigin[i];
                float d = localDir[i];
                float mi = min[i];
                float ma = max[i];

                if (MathF.Abs(d) < 1e-8f)
                {
                    if (o < mi || o > ma) return false;
                    continue;
                }

                float invD = 1f / d;
                float t1 = (mi - o) * invD;
                float t2 = (ma - o) * invD;
                if (t1 > t2)
                {
                    float tmp = t1;
                    t1 = t2;
                    t2 = tmp;
                }

                if (t1 > tMin) tMin = t1;
                if (t2 < tMax) tMax = t2;
                if (tMin > tMax) return false;
            }

            distance = tMin < 0 ? tMax : tMin;
            return distance >= 0 && distance <= Length;
        }

        public Ray CalculateRicochetRay(Vector3 hitPos, Vector3SByte hitNormal, float length)
        {
            const float eps = 0.002f;
            var reflectionDir = CalculateRicochet(this, hitPos, hitNormal);
            var newOrigin = hitPos + reflectionDir * eps;
            return new Ray(newOrigin, reflectionDir, length);
        }

        public static Vector3 CalculateRicochet(Ray ray, Vector3 hitPos, Vector3SByte hitNormal)
        {
            const float eps = 1.01f;
            var n = new Vector3(hitNormal.X, hitNormal.Y, hitNormal.Z);
            n = (n * eps).Normalized();
            var incoming = ray.Direction.Normalized();
            var reflection = incoming - 2f * Vector3.Dot(incoming, n) * n;
            return reflection.Normalized();
        }

        public static float CalculateIncidentAngle(Ray ray, Vector3SByte hitNormal)
        {
            var n = new Vector3(hitNormal.X, hitNormal.Y, hitNormal.Z).Normalized();
            var incoming = -ray.Direction.Normalized();
            var dot = Vector3.Dot(incoming, n);
            dot = Math.Clamp(dot, -1f, 1f);
            var angleRad = MathF.Acos(dot);
            var angleDeg = MathHelper.RadiansToDegrees(angleRad);
            return 90f - angleDeg;
        }
    }
}
