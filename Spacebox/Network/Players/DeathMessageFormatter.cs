namespace Spacebox.Network.Players;

public static class DeathMessageFormatter
{
    public const int MaxReasonLength = 80;
    public const string CommandReason = "Killed by the command";
    public const string HitReason = "Hit by the command";

    public static string Format(string victim, string killer, string reason)
    {
        if (!string.IsNullOrWhiteSpace(killer)) return $"{victim} was killed by {killer}";
        if (reason == CommandReason) return Killed(victim);

        string cleaned = Sanitize(reason);
        return cleaned.Length > 0 ? $"{victim} died: {cleaned}" : $"{victim} died";
    }

    public static string Killed(string victim) => $"{victim} was killed by the command";

    public static string Sanitize(string reason) => ChatText.Clean(reason, MaxReasonLength);
}
