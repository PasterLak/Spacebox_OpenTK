using Engine;
using Engine.Audio;
using Engine.Light;
using OpenTK.Mathematics;

namespace Spacebox.Game.Physics.Explosions;

public sealed class ExplosionEffects : IDisposable
{
    private const float ReferenceRadius = 4f;
    private const float FlashDuration = 0.35f;
    private const int SoundSources = 4;

    private readonly ParticleSystem _fire;
    private readonly ParticleSystem _smoke;
    private readonly ParticleSystem _sparks;
    private readonly SphereEmitter _fireEmitter;
    private readonly SphereEmitter _smokeEmitter;
    private readonly SphereEmitter _sparkEmitter;
    private readonly LightPool<PointLight> _lightPool = new LightPool<PointLight>(2);
    private readonly List<Flash> _flashes = new List<Flash>();
    private readonly AudioSource[] _sounds = new AudioSource[SoundSources];
    private int _nextSound;

    public ExplosionEffects()
    {
        var texture = Resources.Load<Texture2D>(Spacebox.Game.Effects.EffectTextures.White);

        _fireEmitter = new SphereEmitter
        {
            LifeMin = 0.35f,
            LifeMax = 0.8f,
            ColorStart = new Vector4(1f, 0.85f, 0.35f, 1f),
            ColorEnd = new Vector4(0.7f, 0.1f, 0f, 0f),
            RotationSpeedMin = -90f,
            RotationSpeedMax = 90f,
            Center = Vector3.Zero
        };

        _smokeEmitter = new SphereEmitter
        {
            LifeMin = 1.5f,
            LifeMax = 3f,
            ColorStart = new Vector4(0.35f, 0.33f, 0.3f, 0.6f),
            ColorEnd = new Vector4(0.15f, 0.15f, 0.15f, 0f),
            RotationSpeedMin = -30f,
            RotationSpeedMax = 30f,
            Center = Vector3.Zero
        };

        _sparkEmitter = new SphereEmitter
        {
            LifeMin = 0.3f,
            LifeMax = 0.9f,
            ColorStart = new Vector4(1f, 1f, 0.7f, 1f),
            ColorEnd = new Vector4(1f, 0.5f, 0.1f, 0f),
            Center = Vector3.Zero
        };

        _fire = CreateSystem(texture, _fireEmitter, 600);
        _smoke = CreateSystem(texture, _smokeEmitter, 400);
        _sparks = CreateSystem(texture, _sparkEmitter, 800);

        for (int i = 0; i < _sounds.Length; i++)
            _sounds[i] = new AudioSource(Resources.Load<AudioClip>("arExplosion"));
    }

    public void Play(Vector3 center, float radius)
    {
        float scale = radius / ReferenceRadius;

        Configure(_fireEmitter, radius * 0.25f, radius * 1.2f, radius * 2.2f, 0.5f * scale, 1.1f * scale, 1.4f * scale, 2.4f * scale);
        Configure(_smokeEmitter, radius * 0.4f, radius * 0.3f, radius * 0.8f, 0.8f * scale, 1.4f * scale, 2.5f * scale, 4f * scale);
        Configure(_sparkEmitter, radius * 0.1f, radius * 3f, radius * 6f, 0.05f, 0.12f, 0.01f, 0.03f);

        Burst(_fire, _fireEmitter, center, (int)(60 * scale) + 40);
        Burst(_smoke, _smokeEmitter, center, (int)(30 * scale) + 20);
        Burst(_sparks, _sparkEmitter, center, (int)(80 * scale) + 40);

        StartFlash(center, radius);
        PlaySound(center, scale);
    }

    public void Update()
    {
        _fire.Update();
        _smoke.Update();
        _sparks.Update();
        UpdateFlashes();
    }

    public void Render()
    {
        _smoke.Render();
        _fire.Render();
        _sparks.Render();
    }

    public void Dispose()
    {
        _fire.Destroy();
        _smoke.Destroy();
        _sparks.Destroy();

        for (int i = 0; i < _flashes.Count; i++)
            _lightPool.PutBack(_flashes[i].Light);
        _flashes.Clear();

        for (int i = 0; i < _sounds.Length; i++)
            _sounds[i]?.Dispose();
    }

    private static ParticleSystem CreateSystem(Texture2D texture, SphereEmitter emitter, int max)
    {
        return new ParticleSystem(new ParticleMaterial(texture), emitter)
        {
            Max = max,
            Rate = 0,
            Space = SimulationSpace.World,
            UseManually = false,
            Duration = float.PositiveInfinity
        };
    }

    private static void Configure(SphereEmitter emitter, float spawnRadius, float speedMin, float speedMax, float startMin, float startMax, float endMin, float endMax)
    {
        emitter.Radius = spawnRadius;
        emitter.SpeedMin = speedMin;
        emitter.SpeedMax = speedMax;
        emitter.StartSizeMin = startMin;
        emitter.StartSizeMax = startMax;
        emitter.EndSizeMin = endMin;
        emitter.EndSizeMax = endMax;
    }

    private static void Burst(ParticleSystem system, SphereEmitter emitter, Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var particle = emitter.Create();
            particle.Position = center + particle.Position;
            system.PushParticle(particle);
        }
    }

    private void StartFlash(Vector3 center, float radius)
    {
        var light = _lightPool.Take();
        light.Position = center;
        light.Range = radius * 4f;
        light.Diffuse = new Vector3(1f, 0.7f, 0.3f) * 3f;
        light.Specular = Vector3.Zero;
        light.Enabled = true;
        _flashes.Add(new Flash { Light = light, TimeLeft = FlashDuration, Range = radius * 4f });
    }

    private void UpdateFlashes()
    {
        float deltaTime = Time.Delta;

        for (int i = _flashes.Count - 1; i >= 0; i--)
        {
            var flash = _flashes[i];
            flash.TimeLeft -= deltaTime;

            if (flash.TimeLeft <= 0f)
            {
                _lightPool.PutBack(flash.Light);
                _flashes.RemoveAt(i);
                continue;
            }

            float k = flash.TimeLeft / FlashDuration;
            flash.Light.Diffuse = new Vector3(1f, 0.7f, 0.3f) * (3f * k);
            flash.Light.Range = flash.Range * (0.5f + 0.5f * k);
            _flashes[i] = flash;
        }
    }

    private void PlaySound(Vector3 center, float scale)
    {
        var sound = _sounds[_nextSound];
        _nextSound = (_nextSound + 1) % _sounds.Length;

        sound.Position = center;
        sound.Volume = Math.Clamp(0.6f + 0.2f * scale, 0f, 1f);
        sound.Pitch = Math.Clamp(1.1f - 0.1f * scale, 0.6f, 1.2f);
        sound.Play();
    }

    private struct Flash
    {
        public PointLight Light;
        public float TimeLeft;
        public float Range;
    }
}
