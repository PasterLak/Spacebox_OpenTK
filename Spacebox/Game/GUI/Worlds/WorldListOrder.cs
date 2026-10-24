using System.Globalization;

namespace Spacebox.Game.GUI;

public static class WorldListOrder
{
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    public static void Sort(List<WorldInfo> worlds)
    {
        worlds.Sort(Compare);
    }

    public static int Compare(WorldInfo a, WorldInfo b)
    {
        int byTime = LastActivity(b).CompareTo(LastActivity(a));
        return byTime != 0 ? byTime : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
    }

    public static DateTime LastActivity(WorldInfo world)
    {
        var opened = Parse(world.LastOpenedDate);
        var edited = Parse(world.LastEditDate);
        return opened > edited ? opened : edited;
    }

    public static DateTime Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return DateTime.MinValue;
        if (DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact)) return exact;
        return DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var any) ? any : DateTime.MinValue;
    }
}
