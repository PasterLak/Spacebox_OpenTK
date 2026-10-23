namespace Spacebox.Game.Resource.Content;

public static class GameSetVersion
{
    private static readonly char[] Separators = { '.', '-', '_', ' ' };

    public static int Compare(string a, string b)
    {
        var left = Parts(a);
        var right = Parts(b);
        int count = Math.Max(left.Length, right.Length);
        for (int i = 0; i < count; i++)
        {
            string x = i < left.Length ? left[i] : "0";
            string y = i < right.Length ? right[i] : "0";
            bool xNumber = long.TryParse(x, out long xValue);
            bool yNumber = long.TryParse(y, out long yValue);
            int result = xNumber && yNumber ? xValue.CompareTo(yValue) : string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
            if (result != 0) return Math.Sign(result);
        }
        return 0;
    }

    private static string[] Parts(string version)
    {
        string text = (version ?? "").Trim().TrimStart('v', 'V');
        return text.Length == 0 ? Array.Empty<string>() : text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
    }
}
