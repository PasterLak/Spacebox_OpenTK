namespace Spacebox.Game.Player;

public static class ProjectileTargets
{
    public const int NoIgnoredPeer = int.MinValue;

    public static int IgnoredPeer(int ownerPeer, int ricochets) => ricochets > 0 ? NoIgnoredPeer : ownerPeer;

    public static string DeathCause(string attacker, bool self, bool headshot)
    {
        if (self) return headshot ? "Shot themselves in the head" : "Hit by their own shot";
        return headshot ? $"Headshot by {attacker}" : $"Killed by {attacker}";
    }
}
