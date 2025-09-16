using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Effects
{
    public enum PlayerEffectType
    {
        Damage,
        Heal,
        Charge,
        Teleport,
        Custom
    }

    public class PlayerEffects : Node3D
    {
        private readonly Dictionary<PlayerEffectType, ParticleSystem> _effects;
        private readonly Dictionary<PlayerEffectType, bool> _isPlaying;
        private readonly Dictionary<PlayerEffectType, float> _playTime;
        private readonly Dictionary<PlayerEffectType, float> _maxDuration;

        public PlayerEffects()
        {
            _effects = new Dictionary<PlayerEffectType, ParticleSystem>();
            _isPlaying = new Dictionary<PlayerEffectType, bool>();
            _playTime = new Dictionary<PlayerEffectType, float>();
            _maxDuration = new Dictionary<PlayerEffectType, float>();

            InitializeEffects();
        }

        private void InitializeEffects()
        {
            var damageTexture = Resources.Load<Texture2D>("Resources/Textures/damageEffect.png");
            var healTexture = Resources.Load<Texture2D>("Resources/Textures/healEffect.png");
            var chargeTexture = Resources.Load<Texture2D>("Resources/Textures/white.png");
            var teleportTexture = Resources.Load<Texture2D>("Resources/Textures/white.png");
            var customTexture = Resources.Load<Texture2D>("Resources/Textures/white.png");

            _effects[PlayerEffectType.Damage] = CreateEffect(damageTexture, new Vector4(1f, 0f, 0f, 0.4f), new Vector4(1f, 0f, 0f, 0f));
            _effects[PlayerEffectType.Heal] = CreateEffect(healTexture, new Vector4(0f, 1f, 0f, 0.4f), new Vector4(0f, 1f, 0f, 0f));
            _effects[PlayerEffectType.Charge] = CreateEffect(chargeTexture, new Vector4(0f, 0f, 1f, 0.4f), new Vector4(0f, 0f, 1f, 0f));
            _effects[PlayerEffectType.Teleport] = CreateEffect(teleportTexture, new Vector4(0.5f, 0f, 1f, 0.6f), new Vector4(0.5f, 0f, 1f, 0f));
            _effects[PlayerEffectType.Custom] = CreateEffect(customTexture, new Vector4(1f, 1f, 1f, 0.4f), new Vector4(1f, 1f, 1f, 0f));

            _maxDuration[PlayerEffectType.Damage] = 1.5f;
            _maxDuration[PlayerEffectType.Heal] = 2.0f;
            _maxDuration[PlayerEffectType.Charge] = 2.5f;
            _maxDuration[PlayerEffectType.Teleport] = 1.0f;
            _maxDuration[PlayerEffectType.Custom] = 1.5f;

            foreach (var effect in _effects.Values)
            {
                AddChild(effect);
            }
        }

        private ParticleSystem CreateEffect(Texture2D texture, Vector4 startColor, Vector4 endColor)
        {
            var emitter = new SphereEmitter
            {
                SpeedMin = 0.7f,
                SpeedMax = 1f,
                LifeMin = 0.8f,
                LifeMax = 1.8f,
                StartSizeMin = 0.1f,
                StartSizeMax = 0.2f,
                EndSizeMin = 0.01f,
                EndSizeMax = 0.1f,
                AccelerationStart = Vector3.Zero,
                AccelerationEnd = Vector3.Zero,
                RotationSpeedMin = 0f,
                RotationSpeedMax = 180f,
                ColorStart = startColor,
                ColorEnd = endColor,
                Center = Vector3.Zero,
                Radius = 0.2f
            };

            texture.FilterMode = FilterMode.Nearest;

            var system = new ParticleSystem(new ParticleMaterial(texture), emitter)
            {
                Rate = 0,
                Max = 40,
                Space = SimulationSpace.Local,
                Position = Vector3.Zero - Vector3.UnitZ / 10f,
                
            };
            

            return system;
        }

        public void PlayEffect(PlayerEffectType effectType, Vector3? customColor = null)
        {
            if (!_effects.TryGetValue(effectType, out var system)) return;

            if (effectType == PlayerEffectType.Custom && customColor.HasValue)
            {
                var emitter = system.Emitter as SphereEmitter;
                if (emitter != null)
                {
                    emitter.ColorStart = new Vector4(customColor.Value, 0.4f);
                    emitter.ColorEnd = new Vector4(customColor.Value, 0f);
                }
            }

            system.Restart();
            system.Rate = 320;
            _isPlaying[effectType] = true;
            _playTime[effectType] = 0f;
        }

        public override void Update()
        {
            base.Update();

            var keysToStop = new List<PlayerEffectType>();

            foreach (var kvp in _isPlaying)
            {
                if (!kvp.Value) continue;

                var effectType = kvp.Key;
                _playTime[effectType] += Time.Delta;

                var system = _effects[effectType];

                if (system.ParticlesCount >= system.Max)
                {
                    system.Rate = 0;
                }

                if (_playTime[effectType] >= _maxDuration[effectType] && system.ParticlesCount == 0)
                {
                    keysToStop.Add(effectType);
                }
            }

            foreach (var effectType in keysToStop)
            {
                StopEffect(effectType);
            }

            foreach (var kvp in _isPlaying)
            {
                if (kvp.Value)
                {
                    _effects[kvp.Key].Update();
                }
            }
        }

        public override void Render()
        {
            base.Render();

            foreach (var kvp in _isPlaying)
            {
                if (kvp.Value)
                {
                    _effects[kvp.Key].Render();
                }
            }
        }

        private void StopEffect(PlayerEffectType effectType)
        {
            _isPlaying[effectType] = false;
            _playTime[effectType] = 0f;
            _effects[effectType].Rate = 0;
        }

        public bool IsPlaying(PlayerEffectType effectType)
        {
            return _isPlaying.GetValueOrDefault(effectType, false);
        }

        public override void Destroy()
        {
            foreach (var effect in _effects.Values)
            {
                effect.Destroy();
            }

            base.Destroy();
        }
    }
}