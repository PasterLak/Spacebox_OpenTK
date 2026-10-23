namespace Spacebox.Game.Player.GameModes;

public static class GameModeTags
{
    public const int MaxLength = 3;

    public static string DefaultTag(string name)
    {
        string trimmed = (name ?? "").Trim();
        return trimmed.Length == 0 ? "?" : trimmed.Substring(0, 1).ToUpperInvariant();
    }

    public static string DefaultColor(GameMode baseMode) => baseMode switch
    {
        GameMode.Survival => "#4E9A4A",
        GameMode.Creative => "#3F7CC4",
        _ => "#8A8A8A"
    };

    public static string Clamp(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        string trimmed = tag.Trim();
        return trimmed.Length > MaxLength ? trimmed.Substring(0, MaxLength) : trimmed;
    }
}
