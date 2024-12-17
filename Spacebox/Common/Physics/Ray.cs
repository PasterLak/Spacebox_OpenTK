using OpenTK.Mathematics;

namespace Spacebox.Common.Physics
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

            if (tMin > tyMax || tyMin > tMax)
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

            if (tMin > tzMax || tzMin > tMax)
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
