using Engine;
using OpenTK.Mathematics;


namespace Spacebox.Game.Effects
{
    public class StarsEffect : IDisposable
    {
        public ParticleSystem ParticleSystem { get; private set; }
        private Shader particleShader;
        private Texture2D dustTexture;
        private Camera camera;

        public Vector3 Position { get; set; } = Vector3.Zero;

        public Vector3 EmitterDirection { get; set; } = new Vector3(0, 0, 0);

        public bool Enabled = true;

        public StarsEffect(Camera camera)
        {
            this.camera = camera;
            Initialize();
        }

        private void Initialize()
        {
            dustTexture = Resources.Load<Texture2D>("Resources/Textures/star.png");

            dustTexture.FilterMode = FilterMode.Nearest;
         
            particleShader = Resources.Load<Shader>("Shaders/particle");

            ParticleSystem = new ParticleSystem(new ParticleMaterial(dustTexture),new SphereEmitter());

           /* ParticleSystem = new ParticleSystem(dustTexture, particleShader)
            {
                Position = Position,
                UseLocalCoordinates = false,
                Max = 50,
                Rate = 0f
            };*/

            /*  ParticleSystem.AllowDebug = false;

              var emitter = new Emitter(ParticleSystem)
              {
                  SpeedMin = 0f,
                  SpeedMax = 0f,
                  LifetimeMin = 99999f,
                  LifetimeMax = 99999f,
                  SizeMin = 50f,
                  SizeMax = 50f,
                  StartColorMin = new Vector4(1f, 1f, 1f, 1f),
                  StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                  EndColorMin = new Vector4(1f, 1f, 1f, 1f),
                  EndColorMax = new Vector4(1f, 1f, 1f, 1f),
                  SpawnRadius = 0f
              };*/

           // ParticleSystem.Emitter = emitter;
           // ParticleSystem.Renderer.RandomRotation = false;

            particleShader.Use();
            particleShader.SetInt("particleTexture", 0);
        }

        public void Update()
        {
            if (!Enabled) return;
            ParticleSystem.Position = camera.Position;
            ParticleSystem.Update();
        }

        public void Render()
        {
            if (!Enabled) return;
            //ParticleSystem.Renderer.Material.Shader.SetVector3("color", new Vector3(1, 1, 1));
            //ParticleSystem.Draw(camera);
        }

        public void Dispose()
        {
           // ParticleSystem.Dispose();
            particleShader.Dispose();
        }
    }
}
