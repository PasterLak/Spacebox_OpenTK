namespace Spacebox.Game.Player;

public static class AstronautSkins
{
    public const string Default = "Yellow";

    public static readonly string[] Names = { "Black", "Blue", "Cyan", "Green", "Orange", "Purple", "Red", "White", "Yellow" };

    public static bool TryNormalize(string skin, out string name)
    {
        name = null;
        if (string.IsNullOrWhiteSpace(skin)) return false;

        string trimmed = skin.Trim();
        for (int i = 0; i < Names.Length; i++)
        {
            if (!string.Equals(Names[i], trimmed, StringComparison.OrdinalIgnoreCase)) continue;
            name = Names[i];
            return true;
        }
        return false;
    }

    public static string Normalize(string skin) => TryNormalize(skin, out var name) ? name : Default;

    public static byte IndexOf(string skin)
    {
        string name = Normalize(skin);
        for (int i = 0; i < Names.Length; i++)
            if (Names[i] == name) return (byte)i;
        return 0;
    }

    public static string NameAt(byte index) => index < Names.Length ? Names[index] : Default;
}
