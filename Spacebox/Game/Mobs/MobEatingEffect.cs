using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Network;

namespace Spacebox.Game.Mobs;

public sealed class MobEatingEffect
{
    private const float VisibleDistance = 48f;
    private const float EmitRate = 18f;
    private const int MaxParticles = 48;

    private ParticleSystem? _particles;
    private ConeEmitter? _emitter;
    private float _timeLeft;

    public void Play(Vector3 surface, Vector3 direction, short blockId, float duration)
    {
        if (!CanPlay(surface) || !GameAssets.BlockDusts.TryGetValue(blockId, out var texture)) return;

        Dispose();
        _emitter = CreateEmitter(direction, Vector3.One + BlockLightSampler.Sample(surface));
        _particles = new ParticleSystem(new ParticleMaterial(texture), _emitter)
        {
            Rate = EmitRate,
            Max = MaxParticles,
            Space = SimulationSpace.World,
            Position = surface
        };
        _timeLeft = duration;
    }

    public void Stop()
    {
        _timeLeft = 0f;
    }

    public void Update(float deltaTime)
    {
        if (_particles == null) return;

        _timeLeft -= deltaTime;
        _particles.Rate = _timeLeft > 0f ? EmitRate : 0f;
        _particles.Update();

        if (_timeLeft <= 0f && _particles.ParticlesCount == 0) Dispose();
    }

    public void Render()
    {
        _particles?.Render();
    }

    public void Dispose()
    {
        _particles?.Destroy();
        _particles = null;
        _emitter = null;
    }

    private static bool CanPlay(Vector3 surface)
    {
        if (GameSession.IsDedicated || !Settings.Graphics.EffectsEnabled || Camera.Main == null) return false;
        return Vector3.DistanceSquared(Camera.Main.Position, surface) <= VisibleDistance * VisibleDistance;
    }

    private static ConeEmitter CreateEmitter(Vector3 direction, Vector3 light)
    {
        return new ConeEmitter
        {
            SpeedMin = 0.6f,
            SpeedMax = 1.6f,
            LifeMin = 0.4f,
            LifeMax = 0.8f,
            StartSizeMin = 0.08f,
            StartSizeMax = 0.16f,
            EndSizeMin = 0.02f,
            EndSizeMax = 0.05f,
            RotationSpeedMin = 0f,
            RotationSpeedMax = 180f,
            ColorStart = new Vector4(light, 1f),
            ColorEnd = new Vector4(light * 0.6f, 0f),
            Direction = direction,
            Angle = 35f,
            Height = 0.05f
        };
    }
}
