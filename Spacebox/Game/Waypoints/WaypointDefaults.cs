using OpenTK.Mathematics;

namespace Spacebox.Game.Waypoints;

public static class WaypointDefaults
{
    public const int MaxNameLength = 32;
    public const string NamePrefix = "Waypoint ";

    public static readonly Color4[] Palette =
    {
        new Color4(1f, 0.8f, 0.15f, 1f),
        new Color4(0.35f, 0.85f, 1f, 1f),
        new Color4(0.4f, 0.95f, 0.45f, 1f),
        new Color4(1f, 0.55f, 0.15f, 1f),
        new Color4(1f, 0.45f, 0.8f, 1f),
        new Color4(0.45f, 0.55f, 1f, 1f),
        new Color4(1f, 0.3f, 0.3f, 1f),
        new Color4(1f, 1f, 1f, 1f)
    };

    public static Color4 ColorFor(int index)
    {
        int length = Palette.Length;
        return Palette[((index % length) + length) % length];
    }

    public static string NextName(IEnumerable<string> existing)
    {
        var used = new HashSet<string>(existing ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        int number = 1;
        while (used.Contains(NamePrefix + number)) number++;
        return NamePrefix + number;
    }

    public static string CleanName(string name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name)) return fallback;

        var chars = new char[Math.Min(name.Length, MaxNameLength)];
        int length = 0;
        foreach (char c in name)
        {
            if (char.IsControl(c)) continue;
            chars[length++] = c;
            if (length == chars.Length) break;
        }

        string cleaned = new string(chars, 0, length).Trim();
        return cleaned.Length == 0 ? fallback : cleaned;
    }
}
