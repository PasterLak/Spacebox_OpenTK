
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


        public bool IsFinished => elapsedTime >= duration && particleSystem.ParticlesCount == 0;

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

            var emitter = new SphereEmitter()
            {
                LifeMin = 1f,
                LifeMax = 2f,
                StartSizeMin = 0.02f,
                StartSizeMax = 0.2f,
                ColorStart = new Vector4(1f, 1f, 1f, 1f),

                ColorEnd = new Vector4(0.8f, 0.5f, 0.5f, 0f),

                Radius = 0.4f,
                SpeedMin = 0.005f,
                SpeedMax = 0.1f,
                //Dire = Vector3.UnitY,
            };

            particleSystem = new ParticleSystem(new ParticleMaterial(dustTexture) ,new SphereEmitter());
            particleSystem.Space = SimulationSpace.Local;

        }

        public void Update()
        {
            if (elapsedTime < duration)
            {
                elapsedTime += Time.Delta;
                if (elapsedTime >= duration)
                {
                    particleSystem.Rate = 0f;
                }
            }
            particleSystem.Update();

            if (particleSystem.ParticlesCount == particleSystem.Max)
            {
                particleSystem.Rate = 0f;
            }
        }

        public void Render()
        {

          //  particleSystem.Renderer.Material.Shader.SetVector3("color", color);
          //  particleSystem.Draw(camera);
        }

        public void Dispose()
        {
          //  particleSystem.Dispose();
            //dustTexture.Dispose();
            //particleShader.Dispose();
        }
    }
}
