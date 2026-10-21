namespace Spacebox.Network;

public static class PlayerNameRules
{
    public const int MaxLength = 32;

    private static readonly char[] Forbidden = Path.GetInvalidFileNameChars()
        .Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' })
        .Distinct()
        .ToArray();

    public static string Validate(string name)
    {
        string trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0) return "Invalid name";
        if (trimmed.Length > MaxLength) return $"The name is longer than {MaxLength} characters";
        foreach (char c in trimmed)
        {
            if (char.IsControl(c) || Array.IndexOf(Forbidden, c) >= 0) return $"The name contains a character that is not allowed: '{c}'";
        }
        if (trimmed.EndsWith('.')) return "The name cannot end with a dot";
        return null;
    }
}
