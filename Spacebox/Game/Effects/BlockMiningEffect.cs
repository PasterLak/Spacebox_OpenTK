
using Engine;
using OpenTK.Mathematics;


namespace Spacebox.Game.Effects
{
    public class BlockMiningEffect : IDisposable
    {
        public ParticleSystem ParticleSystem { get; private set; }
        private Shader particleShader;
        private Texture2D dustTexture;
        private Camera camera;
        ParticleSystem ParticleSystem2;
        ConeEmitter emitter1;
        PlaneEmitter emitter2; 
        private float elapsedTime = 0f;
        private const float duration = 2f;

        private Vector3 color = Vector3.One;
        public bool Enabled = false;

        public bool IsFinished => elapsedTime >= duration && ParticleSystem.ParticlesCount == 0;

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


             emitter1 = new ConeEmitter
            {
                SpeedMin = 2f,
                SpeedMax = 3f,
                LifeMin = 0.2f,
                LifeMax = 0.2f,
                StartSizeMin = 0.2f,
                StartSizeMax = 0.3f,
                EndSizeMin = 0.05f,
                EndSizeMax = 0.1f,
                AccelerationStart = new Vector3(0f, 0f, 0f),
                AccelerationEnd = new Vector3(0f, 0f, 0f),
                RotationSpeedMin = 0f,
                RotationSpeedMax = 180f,
                ColorStart = new Vector4(1f, 1f, 0.9f, 1f),
                ColorEnd = new Vector4(0.5441632f, 0.29267818f, 0f, 0f),
                Apex = new Vector3(0f, 0f, 0f),
                Axis = new Vector3(0f, 1f, 0f),
                Angle = 12f,
                Height = 0.1f,
            };

            emitter2 = new PlaneEmitter
            {
                SpeedMin = 0f,
                SpeedMax = 0f,
                LifeMin = 1f,
                LifeMax = 1f,
                StartSizeMin = 0.1f,
                StartSizeMax = 0.2f,
                EndSizeMin = 0.1f,
                EndSizeMax = 0.5f,
                AccelerationStart = new Vector3(0f, 0f, 0f),
                AccelerationEnd = new Vector3(0f, 0f, 0f),
                RotationSpeedMin = 0f,
                RotationSpeedMax = 180f,
                ColorStart = new Vector4(0.26762748f, 0.26760072f, 0.26760072f, 1f),
                ColorEnd = new Vector4(0.3336568f, 0.3336234f, 0.3336234f, 0f),
                Center = new Vector3(0f, 0f, 0f),
                Normal = new Vector3(0f, 1f, 0f),
                Width = 0.2f,
                Height = 0.2f,
                Direction = new Vector3(0f, 1f, 0f),
            };

             ParticleSystem2 = new ParticleSystem(new ParticleMaterial(dustTexture), emitter2)
            {
                Rate = 20,
                Max = 100
            };

            ParticleSystem2.Space = SimulationSpace.World;

            ParticleSystem = new ParticleSystem(new ParticleMaterial(dustTexture), emitter1)
            {
                Rate = 100,
                Max = 200
            };
            ParticleSystem.Space = SimulationSpace.World;

            // ParticleSystem.Space = SimulationSpace.Local;
            /*ParticleSystem = new ParticleSystem(dustTexture, shader)
            {
                Position = position,
                UseLocalCoordinates = false,
                EmitterPositionOffset = Vector3.Zero,
                //EmitterDirection = new Vector3(0, 1, 0), 
                Max = 30,
                Rate = 50f
            };*/

            //particleSystem.Renderer = new ParticleRenderer(texture, particleSystem, shader);



            //ParticleSystem.Emitter = emitter;
            // ParticleSystem.UseLocalCoordinates = false;
            //particleShader = new Shader("Shaders/particleShader");
            particleShader = shader;
            //particleShader.Use();
            //particleShader.SetInt("particleTexture", 0);
        }

        private bool _canDestroyBlock = true;

        public void SetEmitter(bool canDestroyBlock)
        {
            if (_canDestroyBlock == canDestroyBlock) return;
            if (!Settings.Graphics.EffectsEnabled) return;

            _canDestroyBlock = canDestroyBlock;

            if (canDestroyBlock)
            {
               
                ParticleSystem.Emitter.ColorStart = new Vector4(1f, 1f, 0.9f, 1f); 
                ParticleSystem.Emitter.ColorEnd = new Vector4(0.5441632f, 0.29267818f, 0f, 0f);

                //  ParticleSystem.Emitter = emitter;
            }
            else
            {
              
                ParticleSystem.Emitter.ColorStart = new Vector4(1f, 1f, 0f, 1f);
                ParticleSystem.Emitter.ColorEnd = new Vector4(0.5441632f, 0.29267818f, 0f, 0f);
                // ParticleSystem.Emitter = emitter2;
            }
            
        }

        public void Update()
        {
            if (!Settings.Graphics.EffectsEnabled) return;
            if (elapsedTime < duration)
            {
                elapsedTime += Time.Delta;
                if (elapsedTime >= duration)
                {
                   // ParticleSystem.Rate = 0f;
                }
            }
            ParticleSystem.Update();
            emitter2.Direction = emitter1.Direction;
            ParticleSystem2.Update();

            if(ParticleSystem.Enabled)
            {
                ParticleSystem2.Position = ParticleSystem.Position;
                ParticleSystem2.Rate = 20;
            }
            else
            {
                ParticleSystem2.Rate = 0;
            }
           // ParticleSystem.Renderer.RandomRotation = true;

            if (ParticleSystem.ParticlesCount == ParticleSystem.Max)
            {
               // ParticleSystem.Rate = 0f;
            }
        }

        public void Render()
        {
            if(!Enabled) return;

            if(!Settings.Graphics.EffectsEnabled) return;

            ParticleSystem.Render();
            ParticleSystem2.Render();
        }

        public void ClearParticles()
        {
            ParticleSystem.ClearParticles();
            ParticleSystem2.ClearParticles();

        }

        public void Dispose()
        {
            ParticleSystem.Destroy();
            ParticleSystem2.Destroy();
            //dustTexture.Dispose();
            //particleShader.Dispose();
        }
    }
}
