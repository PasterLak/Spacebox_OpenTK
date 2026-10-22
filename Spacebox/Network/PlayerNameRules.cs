namespace Spacebox.Network;

public static class PlayerNameRules
{
    public const int MaxLength = 32;

    private static readonly char[] Forbidden = Path.GetInvalidFileNameChars()
        .Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' })
        .Distinct()
        .ToArray();

    private static readonly HashSet<string> ReservedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

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
        if (IsReservedFileName(trimmed)) return "This name is reserved";
        return null;
    }

    private static bool IsReservedFileName(string name)
    {
        int dot = name.IndexOf('.');
        return ReservedFileNames.Contains((dot < 0 ? name : name.Substring(0, dot)).TrimEnd());
    }
}
