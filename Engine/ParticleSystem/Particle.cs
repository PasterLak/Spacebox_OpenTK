using OpenTK.Mathematics;

namespace Engine
{
    public class Particle
    {
        public float StartSize;
        public float EndSize;

        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 AccStart;
        public Vector3 AccEnd;
        public float Rotation;
        public float RotationSpeed;
        public float Life;
        public float Age;
        public Vector4 ColorStart;
        public Vector4 ColorEnd;
        public Vector4 Color;
        public float Size;
        public bool Alive => Age < Life;

        public Particle(
            Vector3 pos,
            Vector3 vel,
            float life,
            Vector4 c0,
            Vector4 c1,
            float startSize,
            float endSize)
        {
            Position = pos;
            Velocity = vel;
            AccStart = Vector3.Zero;
            AccEnd = Vector3.Zero;
            Rotation = 0f;
            RotationSpeed = 0f;
            Life = life;
            Age = 0f;
            ColorStart = c0;
            ColorEnd = c1;
            Color = c0;
            StartSize = startSize;
            EndSize = endSize;
            Size = startSize;
        }

        public void Update()
        {
            float dt = Time.Delta;
            Age += dt;
            float t = MathHelper.Clamp(Age / Life, 0f, 1f);
            Vector3 acc = Vector3.Lerp(AccStart, AccEnd, t);
            Velocity += acc * dt;
            Position += Velocity * dt;
            Rotation += RotationSpeed * dt;
            Color = Vector4.Lerp(ColorStart, ColorEnd, t);
            Size = MathHelper.Lerp(StartSize, EndSize, t);
        }
    }
}
