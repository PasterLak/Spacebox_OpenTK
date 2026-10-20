namespace Spacebox.Game.Protection;

public static class ClaimCost
{
    public static float BurnRate(int activeClaims, int extraClaimCostPercent)
    {
        int extra = Math.Max(0, activeClaims - 1);
        return 1f + extra * Math.Max(0, extraClaimCostPercent) / 100f;
    }

    public static int SecondsFor(ClaimSettings settings, string itemId) => MinutesFor(settings, itemId) * 60;

    public static int MinutesFor(ClaimSettings settings, string itemId)
    {
        if (settings?.Payments == null || string.IsNullOrEmpty(itemId)) return 0;
        if (settings.Payments.TryGetValue(itemId, out int minutes)) return minutes;

        int colon = itemId.IndexOf(':');
        return colon >= 0 && settings.Payments.TryGetValue(itemId.Substring(colon + 1), out minutes) ? minutes : 0;
    }

    public static bool CanAdd(float remainingSeconds, int addSeconds, int maxStoredSeconds)
    {
        return addSeconds > 0 && remainingSeconds + addSeconds <= maxStoredSeconds;
    }

    public static string FormatDuration(float seconds)
    {
        if (seconds <= 0f) return "0m";

        var span = TimeSpan.FromSeconds(seconds);
        if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d {span.Hours}h";
        if (span.TotalHours >= 1) return $"{span.Hours}h {span.Minutes}m";
        return $"{Math.Max(1, span.Minutes)}m";
    }
}
