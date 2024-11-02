using OpenTK.Mathematics;
using System;

namespace Spacebox.Common
{
    public class Emitter
    {
        private ParticleSystem particleSystem;
        private Random random = new Random();

        // Параметры эмиттера
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Direction { get; set; } = Vector3.UnitY;
        public float SpeedMin { get; set; } = 1f;
        public float SpeedMax { get; set; } = 3f;
        public float LifetimeMin { get; set; } = 1f;
        public float LifetimeMax { get; set; } = 3f;
        public float SizeMin { get; set; } = 0.1f;
        public float SizeMax { get; set; } = 0.5f;
        public Vector4 ColorMin { get; set; } = new Vector4(1f, 1f, 1f, 1f);
        public Vector4 ColorMax { get; set; } = new Vector4(1f, 1f, 1f, 1f);

        public Emitter(ParticleSystem system)
        {
            particleSystem = system;
        }

        public void Emit()
        {
            Vector3 position = particleSystem.Position + particleSystem.EmitterPositionOffset;
            Vector3 direction = particleSystem.EmitterDirection.Normalized();

            float speed = MathHelper.Lerp(SpeedMin, SpeedMax, (float)random.NextDouble());

            Vector3 velocity = (direction + new Vector3(
                (float)(random.NextDouble() - 0.5) * 0.2f,
                (float)(random.NextDouble() - 0.5) * 0.2f,
                (float)(random.NextDouble() - 0.5) * 0.2f
            ).Normalized()) * speed;

            float lifetime = MathHelper.Lerp(LifetimeMin, LifetimeMax, (float)random.NextDouble());

            float size = MathHelper.Lerp(SizeMin, SizeMax, (float)random.NextDouble());

            Vector4 color = new Vector4(
                MathHelper.Lerp(ColorMin.X, ColorMax.X, (float)random.NextDouble()),
                MathHelper.Lerp(ColorMin.Y, ColorMax.Y, (float)random.NextDouble()),
                MathHelper.Lerp(ColorMin.Z, ColorMax.Z, (float)random.NextDouble()),
                MathHelper.Lerp(ColorMin.W, ColorMax.W, (float)random.NextDouble())
            );

            Particle particle = new Particle(position, velocity, lifetime, color, size);
            particleSystem.AddParticle(particle);
        }

    }
}
