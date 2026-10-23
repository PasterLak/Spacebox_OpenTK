namespace Spacebox.Game.Resource.Content;

public static class GameSetRoles
{
    public static bool IsEmpty(string idOrFolder) => string.Equals(idOrFolder?.Trim(), Globals.GameSet.EmptyId, StringComparison.OrdinalIgnoreCase);

    public static bool IsBasedOnEmpty(string extends) => IsEmpty(extends);

    public static bool IsExtension(string extends) => !string.IsNullOrWhiteSpace(extends) && !IsBasedOnEmpty(extends);
}
