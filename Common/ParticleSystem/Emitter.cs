// Emitter.cs
using OpenTK.Mathematics;
using System;

namespace Spacebox.Common
{
    public class Emitter
    {
        private ParticleSystem particleSystem;
        private Random random = new Random();

        public Vector3 Position { get; set; } = Vector3.Zero;
        public float LifetimeMin { get; set; } = 2f;
        public float LifetimeMax { get; set; } = 4f;
        public float SizeMin { get; set; } = 0.5f;
        public float SizeMax { get; set; } = 1f;
        public Vector4 StartColorMin { get; set; } = new Vector4(1f, 1f, 1f, 0f);
        public Vector4 StartColorMax { get; set; } = new Vector4(1f, 1f, 1f, 0f);
        public Vector4 EndColorMin { get; set; } = new Vector4(1f, 1f, 1f, 0f);
        public Vector4 EndColorMax { get; set; } = new Vector4(1f, 1f, 1f, 0f);
        public float SpawnRadius { get; set; } = 50f;
        public float SpeedMin { get; set; } = 0f;
        public float SpeedMax { get; set; } = 0f;
        private bool UseLocalCoordinates { get; set; } = true;

        public Emitter(ParticleSystem system)
        {
            particleSystem = system;
        }

        public void Emit()
        {
            Vector3 randomPosition = RandomPointInSphere(SpawnRadius);
            Vector3 position = UseLocalCoordinates
                ? particleSystem.Position + randomPosition + particleSystem.EmitterPositionOffset
                : randomPosition;
            float speed = MathHelper.Lerp(SpeedMin, SpeedMax, (float)random.NextDouble());
            Vector3 velocity = Vector3.Zero; // No movement
            float lifetime = MathHelper.Lerp(LifetimeMin, LifetimeMax, (float)random.NextDouble());
            float size = MathHelper.Lerp(SizeMin, SizeMax, (float)random.NextDouble());
            Vector4 startColor = new Vector4(
                MathHelper.Lerp(StartColorMin.X, StartColorMax.X, (float)random.NextDouble()),
                MathHelper.Lerp(StartColorMin.Y, StartColorMax.Y, (float)random.NextDouble()),
                MathHelper.Lerp(StartColorMin.Z, StartColorMax.Z, (float)random.NextDouble()),
                MathHelper.Lerp(StartColorMin.W, StartColorMax.W, (float)random.NextDouble())
            );
            Vector4 endColor = new Vector4(
                MathHelper.Lerp(EndColorMin.X, EndColorMax.X, (float)random.NextDouble()),
                MathHelper.Lerp(EndColorMin.Y, EndColorMax.Y, (float)random.NextDouble()),
                MathHelper.Lerp(EndColorMin.Z, EndColorMax.Z, (float)random.NextDouble()),
                MathHelper.Lerp(EndColorMin.W, EndColorMax.W, (float)random.NextDouble())
            );
            Particle particle = new Particle(position, velocity, lifetime, startColor, endColor, size);
            particleSystem.AddParticle(particle);
        }

        private Vector3 RandomPointInSphere(float radius)
        {
            float u = (float)random.NextDouble();
            float v = (float)random.NextDouble();
            float theta = u * MathF.PI * 2;
            float phi = MathF.Acos(2 * v - 1);
            float r = radius * MathF.Pow((float)random.NextDouble(), 1f / 3f);
            float sinPhi = MathF.Sin(phi);
            float x = r * sinPhi * MathF.Cos(theta);
            float y = r * sinPhi * MathF.Sin(theta);
            float z = r * MathF.Cos(phi);
            return new Vector3(x, y, z);
        }
    }
}
