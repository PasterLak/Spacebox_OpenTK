namespace Spacebox.Game.Commands.Teleport;

public static class TeleportSelector
{
    public const string Self = "@s";
    public const string All = "@a";

    public static bool TrySelect(string selector, ITeleportable sender, IReadOnlyList<ITeleportable> players, List<ITeleportable> results)
    {
        if (string.Equals(selector, Self, StringComparison.OrdinalIgnoreCase))
        {
            if (sender == null) return false;
            results.Add(sender);
            return true;
        }

        if (string.Equals(selector, All, StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < players.Count; i++) results.Add(players[i]);
            return players.Count > 0;
        }

        var player = FindByName(players, selector);
        if (player == null) return false;
        results.Add(player);
        return true;
    }

    public static ITeleportable FindByName(IReadOnlyList<ITeleportable> players, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        string trimmed = name.Trim();
        for (int i = 0; i < players.Count; i++)
        {
            if (string.Equals(players[i].Name, trimmed, StringComparison.OrdinalIgnoreCase)) return players[i];
        }

        return null;
    }

    public static void AddSuggestions(List<string> results)
    {
        results.Add(Self);
        results.Add(All);
    }
}
