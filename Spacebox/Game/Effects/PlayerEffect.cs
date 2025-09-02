using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Effects
{
    public class PlayerEffect : ParticleSystem
    {

        public PlayerEffect(Texture2D texture) : base(new ParticleMaterial(texture), null)
        {
            
            Init();
        }
        private void Init()
        {
            var emitter1 = new SphereEmitter
            {
                SpeedMin = 1f,
                SpeedMax = 1.5f,
                LifeMin = 0.5f,
                LifeMax = 1f,
                StartSizeMin = 0.1f,
                StartSizeMax = 0.5f,
                EndSizeMin = 0.3f,
                EndSizeMax = 1f,
                AccelerationStart = new Vector3(0f, 0f, 0f),
                AccelerationEnd = new Vector3(0f, 0f, 0f),
                RotationSpeedMin = 0f,
                RotationSpeedMax = 180f,
                ColorStart = new Vector4(1f, 0f, 0f, 0.5f),
                ColorEnd = new Vector4(1f, 0f, 0f, 0f),
                Center = new Vector3(0f, 0f, 0f),
                Radius = 0.25f,
            };

            SetEmitter(emitter1, false);

            Rate = 0;
            Max = 30;
            Space = SimulationSpace.Local;

        }

        public override void Update()
        {
            base.Update();

            if (ParticlesCount >= Max)
            {
                Rate = 0;
            }
        }

        public void OnDamage(Vector3 color)
        {
            var emitter1 = new SphereEmitter
            {
                SpeedMin = 0.7f,
                SpeedMax = 1f,
                LifeMin = 0.8f,
                LifeMax = 1.8f,
                StartSizeMin = 0.1f,
                StartSizeMax = 0.2f,
                EndSizeMin = 0.01f,
                EndSizeMax = 0.1f,
                AccelerationStart = new Vector3(0f, 0f, 0f),
                AccelerationEnd = new Vector3(0f, 0f, 0f),
                RotationSpeedMin = 0f,
                RotationSpeedMax = 180f,
                ColorStart = new Vector4(color, 0.4f),
                ColorEnd = new Vector4(color, 0f),
                Center = new Vector3(0f, 0f, 0f),
                Radius = 0.2f,
            };

            SetEmitter(emitter1, false);

            Position = Vector3.Zero - Vector3.UnitZ/10f;
            Restart();
            Rate = 320;
            Max = 40;
            

        }


    }
}
