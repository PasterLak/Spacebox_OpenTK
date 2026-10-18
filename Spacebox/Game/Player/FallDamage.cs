using OpenTK.Mathematics;
using Spacebox.Game.Physics.Gravity;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player;

public sealed class FallDamage
{
    private const string DeathMessage = "Fell from a great height";
    private static readonly FallDamageJSON DefaultSettings = new FallDamageJSON();

    private readonly LocalAstronaut _player;
    private readonly FallImpactSound _sound = new FallImpactSound(false);

    public event Action<int>? Impact;

    public FallDamage(LocalAstronaut player)
    {
        _player = player;
        _player.Gravity.Landed += OnLanded;
    }

    public static int Calculate(float impactSpeed, float gravityStrength, FallDamageJSON settings)
    {
        if (gravityStrength <= 0f || settings.DamagePerBlock <= 0f) return 0;

        float height = impactSpeed * impactSpeed / (2f * gravityStrength);
        float excess = height - settings.SafeHeight;
        return excess > 0f ? (int)MathF.Ceiling(excess * settings.DamagePerBlock) : 0;
    }

    private void OnLanded(float impactSpeed, Vector3 gravity)
    {
        if (!_player.IsAlive || _player.GameMode != GameMode.Survival) return;

        var settings = GravityField.GetDominantSource(_player.Position)?.FallDamage ?? DefaultSettings;
        int damage = Calculate(impactSpeed, gravity.Length, settings);
        if (damage <= 0) return;

        _sound.Play(damage, _player.Position);
        Impact?.Invoke(damage);
        _player.TakeDamage(damage, new DeathCase(DeathMessage));
    }
}
