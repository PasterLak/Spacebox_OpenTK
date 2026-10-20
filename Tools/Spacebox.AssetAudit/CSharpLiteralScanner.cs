using System.Text;

namespace Spacebox.AssetAudit;

public readonly record struct CSharpLiteral(string Text, bool IsInterpolated);

public static class CSharpLiteralScanner
{
    public static List<CSharpLiteral> Scan(string source)
    {
        var literals = new List<CSharpLiteral>();
        int i = 0;
        while (i < source.Length)
        {
            char c = source[i];
            char next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (c == '/' && next == '/')
            {
                i = SkipLine(source, i);
            }
            else if (c == '/' && next == '*')
            {
                int end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? source.Length : end + 2;
            }
            else if (c == '\'')
            {
                i = SkipCharLiteral(source, i);
            }
            else if (TryReadString(source, ref i, out var literal))
            {
                literals.Add(literal);
            }
            else
            {
                i++;
            }
        }
        return literals;
    }

    private static bool TryReadString(string source, ref int i, out CSharpLiteral literal)
    {
        literal = default;
        int start = i;
        bool interpolated = false;
        bool verbatim = false;

        while (start < source.Length && (source[start] == '$' || source[start] == '@'))
        {
            if (source[start] == '$') interpolated = true;
            else verbatim = true;
            start++;
        }

        if (start >= source.Length || source[start] != '"') return false;
        if (start > i && !IsPrefixStart(source, i)) return false;

        if (start + 2 < source.Length && source[start + 1] == '"' && source[start + 2] == '"')
            return ReadRaw(source, ref i, start, interpolated, out literal);

        var text = new StringBuilder();
        int pos = start + 1;
        while (pos < source.Length)
        {
            char c = source[pos];
            if (verbatim && c == '"' && pos + 1 < source.Length && source[pos + 1] == '"')
            {
                text.Append('"');
                pos += 2;
                continue;
            }
            if (!verbatim && c == '\\' && pos + 1 < source.Length)
            {
                text.Append(Unescape(source[pos + 1]));
                pos += 2;
                continue;
            }
            if (c == '"' || (!verbatim && c == '\n')) break;
            text.Append(c);
            pos++;
        }

        i = pos + 1;
        literal = new CSharpLiteral(text.ToString(), interpolated);
        return true;
    }

    private static bool ReadRaw(string source, ref int i, int start, bool interpolated, out CSharpLiteral literal)
    {
        int quotes = 0;
        while (start + quotes < source.Length && source[start + quotes] == '"') quotes++;

        string fence = new string('"', quotes);
        int contentStart = start + quotes;
        int end = source.IndexOf(fence, contentStart, StringComparison.Ordinal);
        if (end < 0) end = source.Length;

        literal = new CSharpLiteral(source.Substring(contentStart, end - contentStart).Trim(), interpolated);
        i = Math.Min(source.Length, end + quotes);
        return true;
    }

    private static bool IsPrefixStart(string source, int i)
    {
        return i == 0 || !(char.IsLetterOrDigit(source[i - 1]) || source[i - 1] == '_');
    }

    private static char Unescape(char c)
    {
        return c switch
        {
            'n' => '\n',
            't' => '\t',
            'r' => '\r',
            '0' => '\0',
            _ => c
        };
    }

    private static int SkipLine(string source, int i)
    {
        int end = source.IndexOf('\n', i);
        return end < 0 ? source.Length : end + 1;
    }

    private static int SkipCharLiteral(string source, int i)
    {
        int pos = i + 1;
        while (pos < source.Length && source[pos] != '\'' && source[pos] != '\n')
        {
            pos += source[pos] == '\\' ? 2 : 1;
        }
        return pos + 1;
    }
}
