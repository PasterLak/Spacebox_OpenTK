
using OpenTK.Mathematics;

using Engine;


namespace Spacebox.Game.Effects
{
    public class BlockDestructionEffect : IDisposable
    {
        private ParticleSystem particleSystem;
        private Shader particleShader;
        private Texture2D dustTexture;
        private Camera camera;

        private float elapsedTime = 0f;
        private const float duration = 2f;

        private Vector3 color = Vector3.One;


        public bool IsFinished => elapsedTime >= duration && particleSystem.GetParticles().Count == 0;

        public BlockDestructionEffect(Camera camera, Vector3 position, Vector3 color, Texture2D texture, Shader shader)
        {
            this.camera = camera;

            Initialize(position, texture, shader);
            this.color = color;

        }

        private void Initialize(Vector3 position, Texture2D texture, Shader shader)
        {
            //dustTexture = new Texture2D("Resources/Textures/blockDust.png", true);
            dustTexture = texture;

            particleSystem = new ParticleSystem(dustTexture, shader)
            {
                Position = position,
                UseLocalCoordinates = false,
                EmitterPositionOffset = Vector3.Zero,
                //EmitterDirection = new Vector3(0, 1, 0), 
                MaxParticles = 30,
                SpawnRate = 1000f
            };

            //particleSystem.Renderer = new ParticleRenderer(texture, particleSystem, shader);

            var emitter = new Emitter(particleSystem)
            {
                LifetimeMin = 1f,
                LifetimeMax = 2f,
                SizeMin = 0.02f,
                SizeMax = 0.2f,
                StartColorMin = new Vector4(1f, 1f, 1f, 1f),
                StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                EndColorMin = new Vector4(0.8f, 0.5f, 0.5f, 0f),
                EndColorMax = new Vector4(1f, 1f, 0.5f, 0f),
                SpawnRadius = 0.4f,
                SpeedMin = 0.005f,
                SpeedMax = 0.1f,
                EmitterDirection = Vector3.UnitY,

                EnableRandomDirection = true,
                RandomUVRotation = true,
            };

            particleSystem.Emitter = emitter;
            particleSystem.UseLocalCoordinates = true;
            //particleShader = new Shader("Shaders/particleShader");
            particleShader = shader;
            //particleShader.Use();
            //particleShader.SetInt("particleTexture", 0);
        }

        public void Update()
        {
            if (elapsedTime < duration)
            {
                elapsedTime += Time.Delta;
                if (elapsedTime >= duration)
                {
                    particleSystem.SpawnRate = 0f;
                }
            }
            particleSystem.Update();

            if (particleSystem.GetParticles().Count == particleSystem.MaxParticles)
            {
                particleSystem.SpawnRate = 0f;
            }
        }

        public void Render()
        {

            particleSystem.Renderer.shader.SetVector3("color", color);
            particleSystem.Draw(camera);
        }

        public void Dispose()
        {
            particleSystem.Dispose();
            //dustTexture.Dispose();
            //particleShader.Dispose();
        }
    }
}
