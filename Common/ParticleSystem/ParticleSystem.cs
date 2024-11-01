using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Spacebox.Common
{
    public class ParticleSystem : Node3D
    {
        private List<Particle> particles = new List<Particle>();
        private Emitter emitter;
        private ParticleRenderer renderer;

        // Настройки системы частиц
        public int MaxParticles { get; set; } = 1000;
        public float SpawnRate { get; set; } = 100f; // Частиц в секунду
        private float spawnAccumulator = 0f;

        // Настройки эмиттера
        public Vector3 EmitterPositionOffset { get; set; } = Vector3.Zero;
        public Vector3 EmitterDirection { get; set; } = Vector3.UnitY;

        public ParticleSystem(Shader shader, Texture2D texture)
        {
            emitter = new Emitter(this);
            renderer = new ParticleRenderer(shader, texture, this);
        }

        public void Update()
        {
          

            float deltaTime = Time.Delta;

            // Генерация частиц
            spawnAccumulator += SpawnRate * deltaTime;
            while (spawnAccumulator >= 1f)
            {
                emitter.Emit();
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
    }
}
