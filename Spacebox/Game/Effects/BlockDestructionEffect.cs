
using Engine;
using OpenTK.Mathematics;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Spacebox.Game.Effects
{
    public class BlockDestructionEffect : IDisposable
    {
        private ParticleSystem particleSystem;
  
        private float elapsedTime = 0f;
        private const float duration = 2f;

        private Color3Byte color = Color3Byte.White;


        public bool IsFinished => elapsedTime >= duration && particleSystem.ParticlesCount == 0;

        public BlockDestructionEffect(Vector3 position, Color3Byte color, ParticleMaterial material)
        {
            Initialize(position, material);
            this.color = color;

        }

        private void Initialize(Vector3 position, ParticleMaterial material)
        {
            
            var emitter = new SphereEmitter()
            {
                LifeMin = 1.5f,
                LifeMax = 2f,
                StartSizeMin = 0.01f,
                StartSizeMax = 0.2f,
                ColorStart = new Vector4(1f, 1f, 1f, 0.9f),

                ColorEnd = new Vector4(0.8f, 0.6f, 0.6f, 0f),

                Radius = 0.4f,
                SpeedMin = 0.005f,
                SpeedMax = 0.1f,
                //Dire = Vector3.UnitY,
            };

            particleSystem = new ParticleSystem(material, emitter);
        
            particleSystem.Rate = 120f;
            particleSystem.Max = 20;
            particleSystem.Space = SimulationSpace.World;
            particleSystem.Position = position;
           
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

            if (particleSystem.ParticlesCount >= particleSystem.Max)
            {
                particleSystem.Rate = 0f;
            }

            if(particleSystem.ParticlesCount == 0)
            {
                particleSystem.Duration = 0;
                particleSystem.ClearParticles();

            }
        }

        public void Render()
        {
            particleSystem.Render();
           
        }

        public void Dispose()
        {
         
        }
    }
}
