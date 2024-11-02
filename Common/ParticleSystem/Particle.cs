using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Age;
        public float Lifetime;
        public Vector4 Color;
        public float Size;

        public bool IsAlive => Age < Lifetime;

        public Particle(Vector3 position, Vector3 velocity, float lifetime, Vector4 color, float size)
        {
            Position = position;
            Velocity = velocity;
            Lifetime = lifetime;
            Age = 0f;
            Color = color;
            Size = size;
        }

        public void Update(float deltaTime)
        {
            Age += deltaTime;
            if (IsAlive)
            {
                Position += Velocity * deltaTime;
                
                // Velocity += gravity * deltaTime;
            }
        }

        public float GetAlpha()
        {
            // Плавное исчезновение
            return MathHelper.Clamp(1f - (Age / Lifetime), 0f, 1f);
        }
    }
}
