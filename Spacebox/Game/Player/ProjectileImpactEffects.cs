using Spacebox.Game.Generation;
using Spacebox.Network;

namespace Spacebox.Game.Player;

public static class ProjectileImpactEffects
{
    private static readonly Action<Projectile> SpawnSphere = OnDespawn;

    public static void Attach(Projectile projectile)
    {
        if (projectile == null) return;

        projectile.OnDespawn -= SpawnSphere;
        if (projectile.Parameters.ImpactSphere && !GameSession.IsDedicated) projectile.OnDespawn += SpawnSphere;
    }

    private static void OnDespawn(Projectile projectile)
    {
        projectile.OnDespawn -= SpawnSphere;
        SpheresPool.Instance?.Take().Activate(projectile.Position);
        World.Instance?.Explosions?.PlayEffect(projectile.Position, projectile.Parameters.ImpactExplosionRadius);
    }
}
