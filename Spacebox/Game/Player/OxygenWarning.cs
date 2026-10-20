namespace Spacebox.Game.Player;

public static class OxygenWarning
{
    public const float ThresholdFraction = 0.2f;

    public static float Level(int oxygen, int maxOxygen)
    {
        if (maxOxygen <= 0) return 0f;

        float threshold = maxOxygen * ThresholdFraction;
        return Math.Clamp((threshold - oxygen) / threshold, 0f, 1f);
    }

    public static float For(LocalAstronaut player)
    {
        if (player == null || !player.IsAlive || player.GameMode != GameModes.GameMode.Survival) return 0f;

        var oxygen = player.OxygenBar.StatsData;
        return Level(oxygen.Value, oxygen.MaxValue);
    }
}
