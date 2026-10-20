using System.Text;
using System.Text.RegularExpressions;

namespace Spacebox.AssetAudit;

public sealed class GlobPattern
{
    private readonly Regex _regex;

    public string Pattern { get; }

    public GlobPattern(string pattern)
    {
        Pattern = AssetPath.Normalize(pattern.Trim());
        _regex = new Regex("^" + ToRegex(Pattern) + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public bool IsMatch(string relativePath) => _regex.IsMatch(AssetPath.Normalize(relativePath));

    public static List<GlobPattern> ParseLines(IEnumerable<string> lines)
    {
        var patterns = new List<GlobPattern>();
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            patterns.Add(new GlobPattern(trimmed));
        }
        return patterns;
    }

    private static string ToRegex(string pattern)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '*' && i + 1 < pattern.Length && pattern[i + 1] == '*')
            {
                builder.Append(".*");
                i++;
                if (i + 1 < pattern.Length && pattern[i + 1] == '/') i++;
            }
            else if (c == '*') builder.Append("[^/]*");
            else if (c == '?') builder.Append("[^/]");
            else builder.Append(Regex.Escape(c.ToString()));
        }
        return builder.ToString();
    }
}
