// ParticleSystem.cs
using OpenTK.Mathematics;


namespace Spacebox.Common
{
    public class ParticleSystem : Node3D, IDisposable
    {
        private List<Particle> particles = new List<Particle>();
        public Emitter Emitter { get; set; }
        public ParticleRenderer Renderer { get; private set; }

        public int MaxParticles { get; set; } = 1000;
        public float SpawnRate { get; set; } = 100f;
        private float spawnAccumulator = 0f;

        public Vector3 EmitterPositionOffset { get; set; } = Vector3.Zero;
        public Vector3 EmitterDirection { get; set; } = Vector3.UnitY;
        public bool UseLocalCoordinates { get; set; } = true;

        public ParticleSystem(Texture2D texture)
        {
            Emitter = new Emitter(this)
            {
                LifetimeMin = 2f,
                LifetimeMax = 4f,
                SizeMin = 0.5f,
                SizeMax = 1f,
                StartColorMin = new Vector4(1f, 1f, 1f, 0f),
                StartColorMax = new Vector4(1f, 1f, 1f, 0f),
                EndColorMin = new Vector4(1f, 1f, 1f, 0f),
                EndColorMax = new Vector4(1f, 1f, 1f, 0f),
                SpawnRadius = 50f,
                SpeedMin = 0f,
                SpeedMax = 0f
                //UseLocalCoordinates = true
            };
            Renderer = new ParticleRenderer(texture, this, debugShader: false);
        }

        public void Update()
        {
            float deltaTime = Time.Delta;

            spawnAccumulator += SpawnRate * deltaTime;
            while (spawnAccumulator >= 1f)
            {
                Emitter.Emit();
                spawnAccumulator -= 1f;
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Update(deltaTime);
                if (!particles[i].IsAlive)
                {
                    particles.RemoveAt(i);
                }
            }

            Renderer.UpdateBuffers();
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
            Renderer.Render(camera);

            Debug.DrawBoundingBox(new BoundingBox(Position, Vector3.One), Color4.Cyan);
        }

        public void Dispose()
        {
            Renderer.Dispose();
        }
    }
}
