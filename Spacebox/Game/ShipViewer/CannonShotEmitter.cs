using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.ShipViewer;

public sealed class CannonShotEmitter : IDisposable
{
    private const int MaxParticles = 4000;
    private const int TrailParticles = 6;
    private const int FlashParticles = 8;
    private const float FlashLife = 0.12f;
    private const float MinLife = 0.2f;

    private static readonly Vector4 FlashStart = new Vector4(1f, 0.85f, 0.5f, 1f);
    private static readonly Vector4 FlashEnd = new Vector4(1f, 0.4f, 0.1f, 0f);

    private readonly ParticleSystem _particles;
    private readonly Random _random = new Random();

    public CannonShotEmitter()
    {
        var texture = Resources.Load<Texture2D>("Resources/Textures/white.png");
        _particles = new ParticleSystem(new ParticleMaterial(texture), new SphereEmitter())
        {
            Max = MaxParticles,
            Rate = 0,
            Space = SimulationSpace.World,
            UseManually = false,
            Duration = float.PositiveInfinity
        };
    }

    public void Fire(Vector3 muzzle, Vector3 direction, in ProjectileParameters projectile, float range)
    {
        float speed = MathF.Max(1f, projectile.Speed);
        float life = MathF.Max(MinLife, range / speed);
        float size = MathF.Max(0.15f, projectile.Thickness * 2f);
        float length = MathF.Max(0.5f, projectile.Length);
        var color = new Vector4(projectile.Color.R, projectile.Color.G, projectile.Color.B, 1f);

        for (int i = 0; i < TrailParticles; i++)
        {
            float t = i / (float)TrailParticles;
            float trailSize = size * (1f - t * 0.6f);
            Push(muzzle - direction * (length * t), direction * speed, life, color, color, trailSize, trailSize);
        }

        for (int i = 0; i < FlashParticles; i++)
        {
            var jitter = new Vector3(_random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f);
            Push(muzzle, (direction + jitter) * 3f, FlashLife * (0.6f + 0.8f * _random.NextSingle()), FlashStart, FlashEnd, size * 2.5f, 0.05f);
        }
    }

    public void Update()
    {
        _particles.Update();
    }

    public void Render()
    {
        _particles.Render();
    }

    public void Dispose()
    {
        _particles.Destroy();
    }

    private void Push(Vector3 position, Vector3 velocity, float life, Vector4 start, Vector4 end, float startSize, float endSize)
    {
        var particle = _particles.CreateParticle(position, velocity, life);
        particle.ColorStart = start;
        particle.ColorEnd = end;
        particle.Color = start;
        particle.StartSize = startSize;
        particle.EndSize = endSize;
        particle.AccStart = Vector3.Zero;
        particle.AccEnd = Vector3.Zero;
        particle.Rotation = 0f;
        particle.RotationSpeed = 0f;
        _particles.PushParticle(particle);
    }
}
