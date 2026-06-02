using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Events;


namespace Spacebox.Game.Effects
{
    public class DustSpawner : ParticleSystem, IEventListener<GraphicsSettingsChangedEvent>
    {

        private int _defaultRate = 40;
        public DustSpawner() : base(new ParticleMaterial(null), null)
        {
            CreateDust(this);
            Name = "DustSpawner";

            EventBus.Subscribe(this);
            ApplySettings(Settings.Graphics.EffectsEnabled);

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

            var dust = GameAssets.LoadResource<Texture2D>("Resources/Textures/Effects/dust.png");
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

        public void OnEvent(ref GraphicsSettingsChangedEvent eventData)
        {
            Debug.Log("Received GraphicsSettingsChangedEvent in DustSpawner");
            ApplySettings(eventData.NewSettings.EffectsEnabled);
        }

        private void ApplySettings(bool effectsEnabled)
        {
            if (effectsEnabled)
            {
                Rate = _defaultRate;
            }
            else
            {
                Rate = 0;
                ClearParticles();
            }
        }
        public override void Destroy()
        {
            EventBus.Unsubscribe(this);
            base.Destroy();
        }


    }
}
