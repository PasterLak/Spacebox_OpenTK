﻿using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class ParticleSystem : Node3D, IDisposable
    {
        private List<Particle> particles = new List<Particle>();
        public Emitter Emitter { get; set; }
        private ParticleRenderer renderer;

        // Настройки системы частиц
        public int MaxParticles { get; set; } = 1000;
        public float SpawnRate { get; set; } = 100f; // Частиц в секунду
        private float spawnAccumulator = 0f;

        // Настройки эмиттера
        public Vector3 EmitterPositionOffset { get; set; } = Vector3.Zero;
        public Vector3 EmitterDirection { get; set; } = Vector3.UnitY;

        public ParticleSystem(Texture2D texture)
        {
            Emitter = new Emitter(this);
            renderer = new ParticleRenderer(texture, this, true);
        }

        public void Update()
        {
          

            float deltaTime = Time.Delta;

            // Генерация частиц
            spawnAccumulator += SpawnRate * deltaTime;
            while (spawnAccumulator >= 1f)
            {
                Emitter.Emit();
                spawnAccumulator -= 1f;
            }

            // Обновление частиц
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Update(deltaTime);
                if (!particles[i].IsAlive)
                {
                    particles.RemoveAt(i);
                }
            }

            Console.WriteLine($"Alive Particles: {particles.Count}");

            renderer.UpdateBuffers();
        }

        public void AddParticle(Particle particle)
        {
            if (particles.Count < MaxParticles)
            {
                particles.Add(particle);
            }
        }

        public List<Particle> GetParticles()
        {
            return particles;
        }

        public void Draw(Camera camera)
        {
            //base.Draw(camera);
            renderer.Render(camera);
        }

        public void Dispose()
        {
            renderer.Dispose();
        }
    }
}
