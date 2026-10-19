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

    public static string Sanitize(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return "";

        var chars = new char[Math.Min(reason.Length, MaxReasonLength)];
        int length = 0;
        for (int i = 0; i < reason.Length && length < chars.Length; i++)
        {
            char c = reason[i];
            chars[length++] = char.IsControl(c) ? ' ' : c;
        }

        return new string(chars, 0, length).Trim();
    }
}
