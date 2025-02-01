using OpenTK.Mathematics;
using Spacebox.Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;

namespace Spacebox.Game.Effects
{
    public class DropEffect : IDisposable
    {
        public ParticleSystem ParticleSystem { get; private set; }
        private Shader particleShader;
        private Texture2D dustTexture;
        private Camera camera;

        private float elapsedTime = 0f;
        public const float duration = 20f;

        private Vector3 color = Vector3.One;

        public bool IsFinished => elapsedTime >= duration && ParticleSystem.GetParticles().Count == 0;
        public Block Block { get; set; }

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                if (ParticleSystem != null)
                    ParticleSystem.Position = _position;
            }
        }

        public Vector3 Velocity { get; set; }


        public void Initialize(Astronaut player, Vector3 position, Vector3 color, Texture2D texture, Shader shader, Block block)
        {
            camera = player;
            Position = position;
            this.color = color;
            Block = block;
            Velocity = Vector3.Zero;
            elapsedTime = 0f;
            dustTexture = texture;
            particleShader = shader;

            InitializeParticleSystem(position, texture, shader);
        }

        private void InitializeParticleSystem(Vector3 position, Texture2D texture, Shader shader)
        {
            if (ParticleSystem != null)
            {
                ParticleSystem.Dispose();
            }

            ParticleSystem = new ParticleSystem(dustTexture, particleShader)
            {
                Position = position,
                UseLocalCoordinates = true,
                EmitterPositionOffset = Vector3.Zero,
                MaxParticles = 1,
                SpawnRate = 100f
            };

            var emitter = new Emitter(ParticleSystem)
            {
                LifetimeMin = 20f,
                LifetimeMax = 20f,
                SizeMin = 0.2f,
                SizeMax = 0.2f,
                StartColorMin = new Vector4(1f, 1f, 1f, 1f),
                StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                EndColorMin = new Vector4(1f, 1f, 1f, 1f),
                EndColorMax = new Vector4(1f, 1f, 1f, 1f),
                SpawnRadius = 0f,
                SpeedMin = 0,
                SpeedMax = 0,
                EmitterDirection = Vector3.UnitY,
                EnableRandomDirection = false,
                RandomUVRotation = false,
            };

            ParticleSystem.Emitter = emitter;
            ParticleSystem.Renderer.RotateUV180();
        }

        public void Update()
        {
            if (elapsedTime < duration)
            {
                elapsedTime += Time.Delta;
                if (elapsedTime >= duration)
                {
                    ParticleSystem.SpawnRate = 0f;
                }
            }

            ParticleSystem.Update();
        }

        public void Render()
        {
            if (particleShader != null)
            {
                particleShader.Use();
                particleShader.SetVector3("color", color);
            }
            ParticleSystem.Draw(camera);
        }

        public void Reset()
        {
            elapsedTime = 0f;
            color = Vector3.One;
            Block = null;
            Position = Vector3.Zero;
            Velocity = Vector3.Zero;
            camera = null;

            if (ParticleSystem != null)
            {
                ParticleSystem.Dispose();
                ParticleSystem = null;
            }

            dustTexture = null;
            particleShader = null;
        }

        public void Dispose()
        {
            ParticleSystem?.Dispose();
        }
    }
}
