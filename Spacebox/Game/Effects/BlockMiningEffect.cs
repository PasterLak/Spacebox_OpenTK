
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Game;


namespace Spacebox.Game.Effects
{
    public class BlockMiningEffect : IDisposable
    {
        public ParticleSystem ParticleSystem { get; private set; }
        private Shader particleShader;
        private Texture2D dustTexture;
        private Camera camera;

        private float elapsedTime = 0f;
        private const float duration = 2f;

        private Vector3 color = Vector3.One;
        public bool Enabled = false;

        public bool IsFinished => elapsedTime >= duration && ParticleSystem.GetParticles().Count == 0;

        public BlockMiningEffect(Camera camera, Vector3 position, Vector3 color, Texture2D texture, Shader shader)
        {
            this.camera = camera;

            Initialize(position, texture, shader);
            this.color = color;

        }

        private void Initialize(Vector3 position, Texture2D texture, Shader shader)
        {
            //dustTexture = new Texture2D("Resources/Textures/blockDust.png", true);
            dustTexture = texture;

            ParticleSystem = new ParticleSystem(dustTexture, shader)
            {
                Position = position,
                UseLocalCoordinates = false,
                EmitterPositionOffset = Vector3.Zero,
                //EmitterDirection = new Vector3(0, 1, 0), 
                MaxParticles = 30,
                SpawnRate = 50f
            };

            //particleSystem.Renderer = new ParticleRenderer(texture, particleSystem, shader);

            var emitter = new Emitter(ParticleSystem)
            {
                LifetimeMin = 0.1f,
                LifetimeMax = 0.2f,
                SizeMin = 0.1f,
                SizeMax = 0.2f,
                StartColorMin = new Vector4(1f, 1f, 0.8f, 1f),
                StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                EndColorMin = new Vector4(1f, 1f, 1f, 0f),
                EndColorMax = new Vector4(1f, 1f, 1f, 0f),
                SpawnRadius = 0.02f,
                SpeedMin = 0.1f,
                SpeedMax = 0.3f,
                EmitterDirection = Vector3.UnitY,

                EnableRandomDirection = true,
                RandomUVRotation = true,
            };

            ParticleSystem.Emitter = emitter;
            ParticleSystem.UseLocalCoordinates = false;
            //particleShader = new Shader("Shaders/particleShader");
            particleShader = shader;
            //particleShader.Use();
            //particleShader.SetInt("particleTexture", 0);
        }

        private bool _canDestroyBlock = true;

        private Emitter emitter;
        private Emitter emitter2;
        public void SetEmitter(bool canDestroyBlock)
        {
            if (_canDestroyBlock == canDestroyBlock) return;

            _canDestroyBlock = canDestroyBlock;

            if (canDestroyBlock)
            {
                if (emitter == null)
                    emitter = new Emitter(ParticleSystem)
                {
                    LifetimeMin = 0.05f,
                    LifetimeMax = 0.2f,
                    SizeMin = 0.05f,
                    SizeMax = 0.35f,
                    StartColorMin = new Vector4(1f, 1f, 0.8f, 1f),
                    StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                    EndColorMin = new Vector4(1f, 1f, 1f, 0f),
                    EndColorMax = new Vector4(1f, 1f, 1f, 0f),
                    SpawnRadius = 0.02f,
                    SpeedMin = 0.1f,
                    SpeedMax = 0.3f,
                    EmitterDirection = Vector3.UnitY,

                    EnableRandomDirection = true,
                    RandomUVRotation = true,
                };

                ParticleSystem.Emitter = emitter;
            }
            else
            {
                if(emitter2 == null)
                 emitter2 = new Emitter(ParticleSystem)
                {
                    LifetimeMin = 0.08f,
                    LifetimeMax = 0.25f,
                    SizeMin = 0.05f,
                    SizeMax = 0.45f,
                    StartColorMin = new Vector4(1f, 1f, 0f, 1f),
                    StartColorMax = new Vector4(1f, 1f, 0f, 1f),
                    EndColorMin = new Vector4(1f, 1f, 0f, 0f),
                    EndColorMax = new Vector4(1f, 1f, 0f, 0f),
                    SpawnRadius = 0.02f,
                    SpeedMin = 0.1f,
                    SpeedMax = 0.3f,
                    EmitterDirection = Vector3.UnitY,

                    EnableRandomDirection = true,
                    RandomUVRotation = true,
                };

                ParticleSystem.Emitter = emitter2;
            }
            
        }

        public void Update()
        {
            if (elapsedTime < duration)
            {
                //elapsedTime += Time.Delta;
                if (elapsedTime >= duration)
                {
                    //ParticleSystem.SpawnRate = 0f;
                }
            }
            ParticleSystem.Update();
            ParticleSystem.Renderer.RandomRotation = true;

            if (ParticleSystem.GetParticles().Count == ParticleSystem.MaxParticles)
            {
                //ParticleSystem.SpawnRate = 0f;
            }
        }

        public void Render()
        {
            if(!Enabled) return;
            ParticleSystem.Renderer.shader.SetVector3("color", color);
            ParticleSystem.Draw(camera);
        }

        public void ClearParticles()
        {
            ParticleSystem.GetParticles().Clear();
            ParticleSystem.Renderer.UpdateBuffers();
        }

        public void Dispose()
        {
            ParticleSystem.Dispose();
            //dustTexture.Dispose();
            //particleShader.Dispose();
        }
    }
}
