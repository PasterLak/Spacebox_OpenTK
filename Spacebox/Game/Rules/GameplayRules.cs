namespace Spacebox.Game.Rules;

public readonly record struct ExplosionRules(bool Explode, bool BreakBlocks, bool DamagePlayers, bool RespectClaims, bool Pvp, int CauserPeer)
{
    public bool DamagesPlayer(int playerPeer) => Explode && DamagePlayers && (Pvp || CauserPeer == GameplayRules.NoPeer || CauserPeer == playerPeer);
}

public static class GameplayRules
{
    public const int NoPeer = -1;

    public static bool AllowsPlayerHit(ServerRules settings, int attackerPeer, int victimPeer)
    {
        return settings.Pvp || attackerPeer == NoPeer || attackerPeer == victimPeer;
    }

    public static ExplosionRules ForExplosion(ServerRules settings, int causerPeer)
    {
        bool explode = settings.Explosions;
        return new ExplosionRules(
            explode,
            explode && settings.ExplosionBlockDamage,
            explode && settings.ExplosionPlayerDamage,
            settings.ExplosionsRespectClaims,
            settings.Pvp,
            causerPeer);
    }
}
