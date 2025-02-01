
using OpenTK.Mathematics;

namespace Spacebox.Engine
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

        public bool IsAlive => Age < Lifetime;

        public Particle(Vector3 position, Vector3 velocity, float lifetime, Vector4 startColor, Vector4 endColor, float size)
        {
            Position = position;
            Velocity = velocity;
            Lifetime = lifetime;
            Age = 0f;
            StartColor = startColor;
            EndColor = endColor;
            Size = size;
        }

        public void Update(float deltaTime)
        {
            Age += deltaTime;
            if (IsAlive)
            {
                Position += Velocity * deltaTime;
            }
        }

        public Vector4 GetCurrentColor()
        {
            float t = MathHelper.Clamp(Age / Lifetime, 0f, 1f);
            return Vector4.Lerp(StartColor, EndColor, t);
        }
    }
}
