namespace Spacebox.Game.Player;

public static class PlayerStatsAccess
{
    public const int Full = -1;

    public static int Target(int max, int amount) => amount < 0 ? max : Math.Clamp(amount, 0, max);

    public static void Restore(LocalAstronaut player, PlayerStat stats, int amount)
    {
        if (player == null || !player.IsAlive) return;

        if (stats.HasFlag(PlayerStat.Health))
        {
            Set(player.HealthBar?.StatsData, amount);
            if (player.HealthBar?.StatsData is { Value: <= 0 }) player.Kill(new DeathCase(Spacebox.Network.Players.DeathMessageFormatter.CommandReason));
        }
        if (stats.HasFlag(PlayerStat.Power)) Set(player.PowerBar?.StatsData, amount);
        if (stats.HasFlag(PlayerStat.Air)) Set(player.OxygenBar?.StatsData, amount);
    }

    public static string Describe(PlayerStat stats) => stats switch
    {
        PlayerStat.Health => "health",
        PlayerStat.Power => "power",
        PlayerStat.Air => "air",
        _ => "health, power and air"
    };

    private static void Set(StatsData data, int amount)
    {
        if (data == null) return;
        data.SetValue(Target(data.MaxValue, amount));
    }
}
