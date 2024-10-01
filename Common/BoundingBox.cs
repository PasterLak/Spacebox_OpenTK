using OpenTK.Mathematics;


namespace Spacebox.Common
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
           
            return Extents.X ;
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

    public class Ray
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; } // Должен быть нормализован
        public float Length { get; set; } // Максимальная длина луча

        /// <summary>
        /// Создаёт новый луч с заданным началом, направлением и длиной.
        /// </summary>
        /// <param name="origin">Начальная точка луча.</param>
        /// <param name="direction">Направление луча (нормализованное).</param>
        /// <param name="length">Максимальная длина луча.</param>
        public Ray(Vector3 origin, Vector3 direction, float length)
        {
            Origin = origin;
            Direction = direction.Normalized();
            Length = length;
        }

        /// <summary>
        /// Проверяет пересечение луча с BoundingSphere.
        /// Возвращает true, если есть пересечение в пределах длины луча, и расстояние до точки пересечения.
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
                float sqrtDiscriminant = MathF.Sqrt(discriminant);
                float t1 = (-b - sqrtDiscriminant) / (2.0f * a);
                float t2 = (-b + sqrtDiscriminant) / (2.0f * a);

                // Проверяем, попадает ли одно из решений в диапазон [0, Length]
                if (t1 >= 0 && t1 <= Length)
                {
                    distance = t1;
                    return true;
                }

                if (t2 >= 0 && t2 <= Length)
                {
                    distance = t2;
                    return true;
                }

                distance = 0f;
                return false;
            }
        }

        /// <summary>
        /// Проверяет пересечение луча с BoundingBox.
        /// Возвращает true, если есть пересечение в пределах длины луча, и расстояние до точки пересечения.
        /// </summary>
        public bool Intersects(BoundingBox box, out float distance)
        {
            distance = 0f;
            float tMin = (box.Min.X - Origin.X) / Direction.X;
            float tMax = (box.Max.X - Origin.X) / Direction.X;

            if (tMin > tMax)
            {
                float temp = tMin;
                tMin = tMax;
                tMax = temp;
            }

            float tyMin = (box.Min.Y - Origin.Y) / Direction.Y;
            float tyMax = (box.Max.Y - Origin.Y) / Direction.Y;

            if (tyMin > tyMax)
            {
                float temp = tyMin;
                tyMin = tyMax;
                tyMax = temp;
            }

            if ((tMin > tyMax) || (tyMin > tMax))
                return false;

            if (tyMin > tMin)
                tMin = tyMin;

            if (tyMax < tMax)
                tMax = tyMax;

            float tzMin = (box.Min.Z - Origin.Z) / Direction.Z;
            float tzMax = (box.Max.Z - Origin.Z) / Direction.Z;

            if (tzMin > tzMax)
            {
                float temp = tzMin;
                tzMin = tzMax;
                tzMax = temp;
            }

            if ((tMin > tzMax) || (tzMin > tMax))
                return false;

            if (tzMin > tMin)
                tMin = tzMin;

            if (tzMax < tMax)
                tMax = tzMax;

            distance = tMin;

            if (distance < 0)
            {
                distance = tMax;
                if (distance < 0)
                    return false;
            }

            if (distance > Length)
                return false;

            return true;
        }
    }




}
