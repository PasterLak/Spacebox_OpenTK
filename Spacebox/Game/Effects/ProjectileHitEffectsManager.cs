using Engine;
using OpenTK.Mathematics;


namespace Spacebox.Game.Effects
{
    public class ProjectileHitEffectsManager : Node3D
    {
        public static ProjectileHitEffectsManager Instance { get; private set; }
        private readonly Dictionary<short, ProjectileHitEffect> _effects;

        public ProjectileHitEffectsManager()
        {
            Instance = this;
            _effects = new Dictionary<short, ProjectileHitEffect>();
            InitializeEffects();
        }

        private void InitializeEffects()
        {
            foreach (var projectile in GameAssets.Projectiles)
            {
                _effects[projectile.Key] = new ProjectileHitEffect(projectile.Value);
            }
        }

        public void PlayHitEffect(Vector3 position, short projectileId)
        {
            if (_effects.TryGetValue(projectileId, out var effect))
            {
                effect.PlayAt(position);
            }
        }

        public override void Update()
        {
            base.Update();
           
            foreach (var effect in _effects.Values)
            {
                effect.Update();
            }
        }

        public override void Render()
        {
            base.Render();
            foreach (var effect in _effects.Values)
            {
                effect.Render();
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            foreach (var effect in _effects.Values)
            {
                effect.Dispose();
            }
            _effects.Clear();
            Instance = null;
        }
    }
}