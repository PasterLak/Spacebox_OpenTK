using OpenTK.Mathematics;

namespace Spacebox.Common
{
    /// <summary>
    /// Базовый класс для ограничивающих объемов.
    /// </summary>
    public abstract class BoundingVolume
    {
        public Vector3 Center { get; set; }
        public abstract bool Intersects(BoundingVolume other);
        public abstract float GetLongestSide();
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

        



        public override bool Intersects(BoundingVolume other)
        {
            if (other is BoundingBox box)
            {
                return (Min.X <= box.Max.X && Max.X >= box.Min.X) &&
                       (Min.Y <= box.Max.Y && Max.Y >= box.Min.Y) &&
                       (Min.Z <= box.Max.Z && Max.Z >= box.Min.Z);
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
            if (Size.X > Size.Y && Size.X > Size.Z) return Size.X;

            return Size.Y > Size.Z ? Size.Y : Size.Z;
        }
    }

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

        public override string ToString()
        {
            return $"Sphere center: {Center}, radius: {Radius}";
        }

        public override float GetLongestSide()
        {
            return Radius;
        }
    }

    public class Ray
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; } // Должен быть нормализован

        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction.Normalized();
        }

        /// <summary>
        /// Проверяет пересечение с BoundingSphere. Возвращает true, если есть пересечение, и расстояние до точки пересечения.
        /// </summary>
        public bool Intersects(BoundingSphere sphere, out float distance)
        {
            Vector3 oc = Origin - sphere.Center;
            float a = Vector3.Dot(Direction, Direction);
            float b = 2.0f * Vector3.Dot(oc, Direction);
            float c = Vector3.Dot(oc, oc) - sphere.Radius * sphere.Radius;
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                distance = 0f;
                return false;
            }
            else
            {
                distance = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
                return true;
            }
        }
    }
}
