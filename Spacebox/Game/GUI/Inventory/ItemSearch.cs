using System.Text;

namespace Spacebox.Game.GUI;

public static class ItemSearch
{
    public const int NoMatch = int.MaxValue;

    private static readonly char[] Space = { ' ' };

    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var builder = new StringBuilder(text.Length);
        bool lastWasSpace = true;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsLetterOrDigit(c))
            {
                if (i > 0 && char.IsUpper(c) && char.IsLower(text[i - 1]) && !lastWasSpace) builder.Append(' ');
                builder.Append(char.ToLowerInvariant(c));
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static int Score(string query, string name, string keywords)
    {
        string q = Normalize(query);
        if (q.Length == 0) return 0;

        string n = Normalize(name);
        if (n == q) return 0;
        if (n.StartsWith(q, StringComparison.Ordinal)) return 1;
        if (n.Contains(" " + q, StringComparison.Ordinal)) return 2;
        if (n.Contains(q, StringComparison.Ordinal)) return 3;

        string all = n + " " + Normalize(keywords);
        var tokens = q.Split(Space, StringSplitOptions.RemoveEmptyEntries);
        if (ContainsAll(n, tokens)) return 4;
        if (ContainsAll(all, tokens)) return 5;
        if (all.Replace(" ", "").Contains(q.Replace(" ", ""), StringComparison.Ordinal)) return 6;
        return NoMatch;
    }

    private static bool ContainsAll(string text, string[] tokens)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!text.Contains(tokens[i], StringComparison.Ordinal)) return false;
        }
        return true;
    }
}
