using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Physics.Explosions;

public sealed class ExplosionForces
{
    private const string DeathMessage = "Blown up";

    private readonly List<(IExplosionDamageable Target, int Damage)> _damageTargets = new List<(IExplosionDamageable, int)>();

    public void Apply(Vector3 center, ExplosiveBlockData data)
    {
        var world = World.Instance;
        if (world == null) return;

        ApplyToPlayer(world.Player, center, data);
        ApplyToBodies(world.Physics, world.Player, center, data);
        World.DropEffectManager?.PushDropsFrom(center, data.PushRadius, data.PushStrength);
    }

    private static void ApplyToPlayer(LocalAstronaut player, Vector3 center, ExplosiveBlockData data)
    {
        if (player == null || !player.IsAlive || !Spacebox.Network.GameSession.HasLocalPlayer) return;

        float distance = Vector3.Distance(player.Position, center);
        player.ApplyImpulse(GetImpulse(player.Position, center, distance, data));

        int damage = (int)MathF.Round(data.GetDamage(distance));
        if (damage > 0 && player.GameMode == GameMode.Survival)
            player.TakeDamage(damage, new DeathCase(DeathMessage));
    }

    private void ApplyToBodies(PhysicsWorld physics, LocalAstronaut player, Vector3 center, ExplosiveBlockData data)
    {
        if (physics == null) return;

        _damageTargets.Clear();
        var bodies = physics.Bodies;

        for (int i = 0; i < bodies.Count; i++)
        {
            var body = bodies[i];
            if (!body.Enabled || body == player?.Body) continue;

            float distance = Vector3.Distance(body.Center, center);
            if (distance > data.PushRadius) continue;

            if (!body.IsKinematic) body.Velocity += GetImpulse(body.Center, center, distance, data);

            int damage = (int)MathF.Round(data.GetDamage(distance));
            if (damage > 0 && body.Owner is IExplosionDamageable target) _damageTargets.Add((target, damage));
        }

        for (int i = 0; i < _damageTargets.Count; i++)
            _damageTargets[i].Target.TakeExplosionDamage(_damageTargets[i].Damage);

        _damageTargets.Clear();
    }

    private static Vector3 GetImpulse(Vector3 position, Vector3 center, float distance, ExplosiveBlockData data)
    {
        if (distance >= data.PushRadius) return Vector3.Zero;

        var direction = distance > 1e-3f ? (position - center) / distance : Vector3.UnitY;
        return direction * (data.PushStrength * (1f - distance / data.PushRadius));
    }
}
