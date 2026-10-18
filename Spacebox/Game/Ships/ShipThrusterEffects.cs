using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Ships;

public sealed class ShipThrusterEffects : IDisposable
{
    private const int MaxParticles = 1500;
    private const float ParticlesPerSecond = 45f;
    private const float ExhaustSpeed = 6f;
    private const float Spread = 0.6f;
    private const float Life = 0.35f;

    private static readonly Vector4 ColorStart = new Vector4(0.6f, 0.85f, 1f, 1f);
    private static readonly Vector4 ColorEnd = new Vector4(0.2f, 0.3f, 1f, 0f);

    private readonly ParticleSystem _flames;
    private readonly Random _random = new Random();

    public ShipThrusterEffects()
    {
        var texture = Resources.Load<Texture2D>("Resources/Textures/white.png");
        _flames = new ParticleSystem(new ParticleMaterial(texture), new SphereEmitter())
        {
            Max = MaxParticles,
            Rate = 0,
            Space = SimulationSpace.World,
            UseManually = false,
            Duration = float.PositiveInfinity
        };
    }

    public void Update(Sector sector, float deltaTime)
    {
        if (sector != null)
        {
            var entities = sector.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.HasShipControl) Emit(entity, deltaTime);
            }
        }

        _flames.Update();
    }

    public void Render()
    {
        _flames.Render();
    }

    public void Dispose()
    {
        _flames.Destroy();
    }

    private void Emit(SpaceEntity entity, float deltaTime)
    {
        var thrusters = entity.ShipControl.Thrusters;
        for (int i = 0; i < thrusters.Count; i++)
        {
            var thruster = thrusters[i];
            if (!thruster.IsFiring || !thruster.IsPowered) continue;
            EmitThruster(entity, thruster, deltaTime);
        }
    }

    private void EmitThruster(SpaceEntity entity, ThrusterBlock thruster, float deltaTime)
    {
        var frame = thruster.Frame;
        float nozzle = MathF.Sqrt(MathF.Max(1f, frame.Size.X * frame.Size.Y * frame.Size.Z / MathF.Max(1f, frame.ExtentAlong(frame.Front) * 2f)));
        float expected = ParticlesPerSecond * nozzle * deltaTime;
        int count = (int)expected + (_random.NextSingle() < expected - (int)expected ? 1 : 0);
        if (count == 0) return;

        var origin = entity.EntityLocalToWorld(thruster.ExhaustPointLocal);
        var direction = entity.DirectionToWorldSpace(frame.Front);
        var carrier = entity.Ship?.GetPointVelocity(origin) ?? Vector3.Zero;
        float radius = nozzle * 0.35f;

        for (int i = 0; i < count; i++)
        {
            var jitter = new Vector3(_random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f);
            var particle = _flames.CreateParticle(origin + jitter * radius, carrier + (direction + jitter * Spread) * ExhaustSpeed * nozzle, Life * (0.7f + 0.6f * _random.NextSingle()));
            particle.ColorStart = ColorStart;
            particle.ColorEnd = ColorEnd;
            particle.Color = ColorStart;
            particle.StartSize = 0.35f * nozzle;
            particle.EndSize = 0.05f;
            particle.AccStart = Vector3.Zero;
            particle.AccEnd = Vector3.Zero;
            particle.Rotation = 0f;
            particle.RotationSpeed = 0f;
            _flames.PushParticle(particle);
        }
    }
}
