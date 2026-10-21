namespace Spacebox.Game.Rules;

public sealed class ServerRules
{
    private static readonly ServerRules SessionRules = new ServerRules();

    public bool Pvp { get; set; } = true;
    public bool Explosions { get; set; } = true;
    public bool ExplosionBlockDamage { get; set; } = true;
    public bool ExplosionPlayerDamage { get; set; } = true;
    public bool ExplosionsRespectClaims { get; set; } = true;

    public static ServerRules Current => Spacebox.Network.Dedicated.DedicatedServer.Config?.Gameplay ?? SessionRules;

    public static void ResetSession() => SessionRules.CopyFrom(new ServerRules());

    public void CopyFrom(ServerRules other)
    {
        Pvp = other.Pvp;
        Explosions = other.Explosions;
        ExplosionBlockDamage = other.ExplosionBlockDamage;
        ExplosionPlayerDamage = other.ExplosionPlayerDamage;
        ExplosionsRespectClaims = other.ExplosionsRespectClaims;
    }
}
