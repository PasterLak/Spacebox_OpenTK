using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Effects;

public static class JetpackNozzles
{
    public const float BackOffset = 0.3f;
    public const float DownOffset = 0.75f;
    public const float SideOffset = 0.12f;

    public static Vector3 Left(Vector3 position, Vector3 front, Vector3 up, Vector3 right) => Center(position, front, up) - right * SideOffset;

    public static Vector3 Right(Vector3 position, Vector3 front, Vector3 up, Vector3 right) => Center(position, front, up) + right * SideOffset;

    public static Vector3 ExhaustDirection(Vector3 front, Vector3 up) => (-front * 0.8f - up * 0.6f).Normalized();

    private static Vector3 Center(Vector3 position, Vector3 front, Vector3 up) => position - front * BackOffset - up * DownOffset;
}

public sealed class JetpackEffects : IDisposable
{
    private const int MaxParticles = 600;
    private const float ParticlesPerSecond = 70f;
    private const float ExhaustSpeed = 4f;
    private const float Spread = 0.35f;
    private const float Life = 0.18f;

    private static readonly Vector4 ColorStart = new Vector4(1f, 0.75f, 0.35f, 1f);
    private static readonly Vector4 ColorEnd = new Vector4(0.4f, 0.55f, 1f, 0f);

    private readonly ParticleSystem _flames;
    private readonly Random _random = new Random();

    public JetpackEffects()
    {
        var texture = Resources.Load<Texture2D>(EffectTextures.White);
        _flames = new ParticleSystem(new ParticleMaterial(texture), new SphereEmitter())
        {
            Max = MaxParticles,
            Rate = 0,
            Space = SimulationSpace.World,
            UseManually = false,
            Duration = float.PositiveInfinity
        };
    }

    public void Update(float deltaTime)
    {
        if (Settings.Graphics.EffectsEnabled)
        {
            var network = NetworkManager.Instance;
            foreach (var networkObject in network.Spawner.Objects)
            {
                if (networkObject.Owner is Astronaut astronaut) TryEmit(astronaut, deltaTime);
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

    private void TryEmit(Astronaut astronaut, float deltaTime)
    {
        if (!astronaut.IsBoosting || astronaut.IsMain || !astronaut.Enabled) return;
        if (astronaut is RemoteAstronaut remote && !remote.IsAliveVisual) return;

        float expected = ParticlesPerSecond * deltaTime;
        int count = (int)expected + (_random.NextSingle() < expected - (int)expected ? 1 : 0);
        if (count == 0) return;

        var direction = JetpackNozzles.ExhaustDirection(astronaut.Front, astronaut.Up);
        var left = JetpackNozzles.Left(astronaut.Position, astronaut.Front, astronaut.Up, astronaut.Right);
        var right = JetpackNozzles.Right(astronaut.Position, astronaut.Front, astronaut.Up, astronaut.Right);

        for (int i = 0; i < count; i++)
        {
            Emit(left, direction);
            Emit(right, direction);
        }
    }

    private void Emit(Vector3 origin, Vector3 direction)
    {
        var jitter = new Vector3(_random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f);
        var particle = _flames.CreateParticle(origin + jitter * 0.04f, (direction + jitter * Spread) * ExhaustSpeed, Life * (0.7f + 0.6f * _random.NextSingle()));
        particle.ColorStart = ColorStart;
        particle.ColorEnd = ColorEnd;
        particle.Color = ColorStart;
        particle.StartSize = 0.09f;
        particle.EndSize = 0.02f;
        particle.AccStart = Vector3.Zero;
        particle.AccEnd = Vector3.Zero;
        particle.Rotation = 0f;
        particle.RotationSpeed = 0f;
        _flames.PushParticle(particle);
    }
}
