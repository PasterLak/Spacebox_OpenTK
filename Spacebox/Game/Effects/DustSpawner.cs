using OpenTK.Mathematics;
using Engine;


namespace Spacebox.Game.Effects
{
    public class DustSpawner : ParticleSystem
    {

        public DustSpawner() : base(new ParticleMaterial(null), null)
        {
            CreateDust(this);
        }

        private static void CreateDust(ParticleSystem system)
        {
            var emitter = new SphereEmitter
            {
                SpeedMin = 0,
                SpeedMax = 0,
                LifeMin = 10f,
                LifeMax = 30f,
                StartSizeMin = 0.03f,
                StartSizeMax = 0.05f,
                EndSizeMin = 0.1f,
                EndSizeMax = 0.2f,
                AccelerationStart = new Vector3(0f, 0f, 0f),
                AccelerationEnd = new Vector3(0f, 0f, 0f),
                RotationSpeedMin = 0f,
                RotationSpeedMax = 180f,
                ColorStart = new Vector4(1f, 1f, 1f, 0f),
                ColorEnd = new Vector4(0.8f, 0.8f, 0.8f, 1f),
                Center = new Vector3(0f, 0f, 0f),
                Radius = 100
            };

            var dust = Resources.Load<Texture2D>("Resources/Textures/dust.png");
            dust.FilterMode = FilterMode.Nearest;

            system.Material.MainTexture = dust;
            system.SetEmitter(emitter, false);
            system.Space = SimulationSpace.World;
            system.Max = 350;
            system.Rate = 40;
            system.Space = SimulationSpace.World;
            system.Position = new Vector3(0, 0, -30);
            system.Rotation = new Vector3(90, 0, 0);
            system.Prewarm(2f);

        }


        public override void Update()
        {

            if (Settings.Graphics.EffectsEnabled == false)
            {
                if (Rate > 0) Rate = 0;
                if(ParticlesCount > 0) ClearParticles();
                return;
            }
        
            base.Update();

        }

    }
}
