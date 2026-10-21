namespace Spacebox.Game.GUI;

public static class NameSuggestions
{
    public static void Filter(string typed, IReadOnlyList<string> candidates, IReadOnlyList<string> exclude, List<string> results, int max)
    {
        results.Clear();
        string text = typed?.Trim() ?? "";

        AddMatches(text, candidates, exclude, results, max, prefix: true);
        if (text.Length > 0) AddMatches(text, candidates, exclude, results, max, prefix: false);
    }

    private static void AddMatches(string text, IReadOnlyList<string> candidates, IReadOnlyList<string> exclude, List<string> results, int max, bool prefix)
    {
        for (int i = 0; i < candidates.Count && results.Count < max; i++)
        {
            string name = candidates[i];
            if (string.IsNullOrWhiteSpace(name) || Contains(results, name) || Contains(exclude, name)) continue;
            if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase)) continue;

            bool matches = prefix
                ? name.StartsWith(text, StringComparison.OrdinalIgnoreCase)
                : name.IndexOf(text, StringComparison.OrdinalIgnoreCase) > 0;
            if (matches) results.Add(name);
        }
    }

    private static bool Contains(IReadOnlyList<string> names, string name)
    {
        if (names == null) return false;
        for (int i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
