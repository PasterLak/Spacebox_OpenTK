using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs;

public sealed class MobDeathEffect : IDisposable
{
    private const float BurstTime = 0.15f;
    private const float DarkFactor = 0.45f;

    private readonly ParticleSystem _sparks;
    private readonly ParticleSystem _puffs;
    private float _elapsed;

    public bool IsFinished => _elapsed >= BurstTime && _sparks.ParticlesCount == 0 && _puffs.ParticlesCount == 0;

    public MobDeathEffect(Texture2D texture, MobDeathParticlesJSON settings, Vector3 position, Vector3 light)
    {
        var color = new Vector3(settings.Color.R, settings.Color.G, settings.Color.B) / 255f * light;
        int sparks = Math.Max(1, settings.Count * 2 / 3);
        int puffs = Math.Max(1, settings.Count - sparks);

        _sparks = Create(texture, position, sparks, new SphereEmitter
        {
            Radius = settings.Radius,
            SpeedMin = settings.Speed * 0.5f,
            SpeedMax = settings.Speed,
            LifeMin = settings.Life * 0.6f,
            LifeMax = settings.Life,
            StartSizeMin = settings.Size * 0.5f,
            StartSizeMax = settings.Size,
            EndSizeMin = 0.01f,
            EndSizeMax = settings.Size * 0.3f,
            RotationSpeedMin = -180f,
            RotationSpeedMax = 180f,
            ColorStart = new Vector4(color * 1.3f, 1f),
            ColorEnd = new Vector4(color, 0f)
        });

        _puffs = Create(texture, position, puffs, new SphereEmitter
        {
            Radius = settings.Radius * 0.6f,
            SpeedMin = settings.Speed * 0.1f,
            SpeedMax = settings.Speed * 0.35f,
            LifeMin = settings.Life,
            LifeMax = settings.Life * 1.6f,
            StartSizeMin = settings.Size * 1.2f,
            StartSizeMax = settings.Size * 2.2f,
            EndSizeMin = settings.Size * 2f,
            EndSizeMax = settings.Size * 3f,
            RotationSpeedMin = -60f,
            RotationSpeedMax = 60f,
            ColorStart = new Vector4(color * DarkFactor, 0.8f),
            ColorEnd = new Vector4(color * DarkFactor, 0f)
        });
    }

    public void Update(float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed >= BurstTime)
        {
            _sparks.Rate = 0f;
            _puffs.Rate = 0f;
        }

        _sparks.Update();
        _puffs.Update();
    }

    public void Render()
    {
        _puffs.Render();
        _sparks.Render();
    }

    public void Dispose()
    {
        _sparks.Destroy();
        _puffs.Destroy();
    }

    private static ParticleSystem Create(Texture2D texture, Vector3 position, int count, SphereEmitter emitter)
    {
        return new ParticleSystem(new ParticleMaterial(texture), emitter)
        {
            Max = count,
            Rate = count / BurstTime,
            Space = SimulationSpace.World,
            Position = position
        };
    }
}
