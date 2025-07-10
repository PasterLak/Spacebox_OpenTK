using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.Player;

namespace Spacebox.Game.Effects
{
    public class DustSpawner : IDisposable
    {
        public ParticleSystem ParticleSystem { get; private set; }
        private Shader particleShader;
        private Texture2D dustTexture;
        private Camera camera;

        public Vector3 Position { get; set; } = Vector3.Zero;

        public Vector3 EmitterDirection { get; set; } = new Vector3(0, 0, 0);

        public DustSpawner(Camera camera)
        {
            this.camera = camera;
            Initialize();
        }

        private void Initialize()
        {

            dustTexture = Resources.Load<Texture2D>("Resources/Textures/dust.png");
      
            dustTexture.FilterMode = FilterMode.Nearest;

            particleShader = Resources.Load<Shader>("Shaders/particle");

            ParticleSystem = new ParticleSystem(new ParticleMaterial(dustTexture), new SphereEmitter());

            /*ParticleSystem = new ParticleSystem(dustTexture, particleShader)
            {
                Position = Position,
                UseLocalCoordinates = false,
                Max = 350,
                Rate = 40f
            };*/

            //ParticleSystem.AllowDebug = false;

            var emitter = new EmitterOld(ParticleSystem)
            {
                SpeedMin = 0f,
                SpeedMax = 0f,
                LifetimeMin = 10f,
                LifetimeMax = 30f,
                SizeMin = 0.05f,
                SizeMax = 0.2f,
                StartColorMin = new Vector4(1f, 1f, 1f, 1f),
                StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                EndColorMin = new Vector4(1f, 1f, 1f, 1f),
                EndColorMax = new Vector4(0.8f, 0.8f, 0.8f, 1f),
                SpawnRadius = 70f
            };

            //ParticleSystem.Emitter = emitter;
            //ParticleSystem.Renderer.RandomRotation = false;

            particleShader.Use();
            particleShader.SetInt("particleTexture", 0);
        }

        public void Update()
        {
            ParticleSystem.Position = camera.Position;
            ParticleSystem.Update();
        }

        public void Render()
        {
           // ParticleSystem.Renderer.Material.Shader.SetVector3("color", new Vector3(1, 1, 1));

            Astronaut ast = camera as Astronaut;

           // if(ast == null)
          //  ParticleSystem.Draw(camera);
          //  else
          //      ParticleSystem.Draw(ast);
        }

        public void Dispose()
        {
         //   ParticleSystem.Dispose();
            particleShader.Dispose();
        }
    }
}
