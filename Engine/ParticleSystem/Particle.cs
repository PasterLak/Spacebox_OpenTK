
using OpenTK.Mathematics;

namespace Engine
{
    public class Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Age;
        public float Lifetime;
        public Vector4 StartColor;
        public Vector4 EndColor;
        public float Size;

        public bool Alive => Age < Lifetime;

        public Particle(Vector3 position, Vector3 velocity, float lifetime, Vector4 startColor, Vector4 endColor, float size)
        {
            Position = position;
            Velocity = velocity;
            Lifetime = lifetime;
            StartColor = startColor;
            EndColor = endColor;
            Size = size;
        }

        public void Update()
        {
            var deltaTime = Time.Delta;

            Age += deltaTime;
            if (Alive)
            {
                Position += (Velocity ) * deltaTime;
            }
        }
        public Vector4 Color => Vector4.Lerp(StartColor, EndColor, MathHelper.Clamp(Age / Lifetime, 0f, 1f));
        public Vector4 GetCurrentColor()
        {
            float t = MathHelper.Clamp(Age / Lifetime, 0f, 1f);
            return Vector4.Lerp(StartColor, EndColor, t);
        }
    }
}
