using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Effects
{
    public class ProjectileHitEffect : IDisposable
    {
        public ParticleSystem ParticleSystem { get; private set; }
        private ProjectileParameters _parameters;
        private SphereEmitter emitter;
        public ProjectileHitEffect(ProjectileParameters parameters)
        {
            _parameters = parameters;
            CreateParticleSystem( parameters);
        }

        private void CreateParticleSystem(ProjectileParameters parameters)
        {
            var thickness = parameters.Thickness;

             emitter = new SphereEmitter
            {
                SpeedMin = 5f,
                SpeedMax = 15f,
                LifeMin = 0.05f,
                LifeMax = 0.1f,
                StartSizeMin = 0.02f + thickness,
                StartSizeMax = 0.05f + thickness,
                EndSizeMin =  0.001f,
                EndSizeMax = 0.005f + thickness,
                RotationSpeedMin = -360f,
                RotationSpeedMax = 360f,
                ColorStart = new Vector4(parameters.Color3, 1f),
                ColorEnd = new Vector4(parameters.Color3, 0f),
                Center = Vector3.Zero,
                Radius = 0.1f
            };

            var sparkTexture = Resources.Load<Texture2D>("Resources/Textures/white.png");
            sparkTexture.FilterMode = FilterMode.Nearest;

            ParticleSystem = new ParticleSystem(new ParticleMaterial(sparkTexture), emitter)
            {
                Max = Math.Clamp(_parameters.DamageBlocks * 5, 50, 200),
                Rate = 0,
                Space = SimulationSpace.World,
                UseManually = false,
                Duration = float.PositiveInfinity
            };
        }

        public void PlayAt(Vector3 position)
        {
            int burstCount = Math.Clamp(_parameters.DamageBlocks * 2, 15, 60);

            for (int i = 0; i < burstCount; i++)
            {
                var particle = emitter.Create();
                particle.Position = position + particle.Position;
                ParticleSystem.PushParticle(particle);
            }
        }

        public void Update() => ParticleSystem.Update();
        public void Render() => ParticleSystem.Render();
        public void Dispose() => ParticleSystem?.Destroy();
    }
}