namespace Spacebox.Game.GUI.Canvas;

public static class GameUiDesigns
{
    public static readonly string[] Ids = { "classic", "modern" };
    public static readonly string[] Names = { "Classic", "Modern" };

    public static bool IsKnown(string id) => Array.IndexOf(Ids, id?.Trim().ToLowerInvariant()) >= 0;

    public static GameUiDesign Parse(string id) => string.Equals(id?.Trim(), "modern", StringComparison.OrdinalIgnoreCase) ? GameUiDesign.Modern : GameUiDesign.Classic;
}
