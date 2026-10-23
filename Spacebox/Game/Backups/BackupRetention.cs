using System.Globalization;

namespace Spacebox.Game.Backups;

public static class BackupRetention
{
    public const string TimeFormat = "yyyyMMdd_HHmmss";

    public static bool TryParse(string fileName, string worldName, string label, out DateTime time)
    {
        time = default;
        string prefix = $"{worldName}_{label}_";
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return false;

        string stamp = fileName.Substring(prefix.Length, fileName.Length - prefix.Length - 4);
        return DateTime.TryParseExact(stamp, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }

    public static List<string> SelectToDelete(IEnumerable<string> fileNames, string worldName, IReadOnlyList<string> labels, int keep)
    {
        var dated = new List<(string Name, DateTime Time)>();
        foreach (string name in fileNames)
        {
            foreach (string label in labels)
            {
                if (!TryParse(name, worldName, label, out var time)) continue;
                dated.Add((name, time));
                break;
            }
        }

        dated.Sort((a, b) => b.Time.CompareTo(a.Time));
        var result = new List<string>();
        for (int i = Math.Max(0, keep); i < dated.Count; i++) result.Add(dated[i].Name);
        return result;
    }
}
