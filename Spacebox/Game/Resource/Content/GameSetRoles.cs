namespace Spacebox.Game.Resource.Content;

public static class GameSetRoles
{
    public static bool IsEmpty(string idOrFolder) => string.Equals(idOrFolder?.Trim(), Globals.GameSet.EmptyId, StringComparison.OrdinalIgnoreCase);

    public static bool IsBasedOnEmpty(string extends)
    {
        var targets = ExtensionTargets.Parse(extends);
        return targets.Count == 1 && IsEmpty(targets[0]);
    }

    public static bool IsExtension(string extends) => !string.IsNullOrWhiteSpace(extends) && !IsBasedOnEmpty(extends);
}
