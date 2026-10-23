namespace Spacebox.Game.Player.GameModes;

public static class GameModeList
{
    public static string Describe()
    {
        var parts = new List<string>();
        foreach (var mode in GameModeCatalog.All) parts.Add($"{(int)mode.Id} {mode.Key}");
        return string.Join(", ", parts);
    }
}
