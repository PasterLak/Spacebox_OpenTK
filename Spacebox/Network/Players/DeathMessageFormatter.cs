namespace Spacebox.Network.Players;

public static class DeathMessageFormatter
{
    public const int MaxReasonLength = 80;

    public static string Format(string victim, string killer, string reason)
    {
        if (!string.IsNullOrWhiteSpace(killer)) return $"{victim} was killed by {killer}";

        string cleaned = Sanitize(reason);
        return cleaned.Length > 0 ? $"{victim} died: {cleaned}" : $"{victim} died";
    }

    public static string Sanitize(string reason) => ChatText.Clean(reason, MaxReasonLength);
}
