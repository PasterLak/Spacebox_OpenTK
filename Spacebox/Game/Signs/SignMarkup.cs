using System.Globalization;
using System.Text;
using OpenTK.Mathematics;

namespace Spacebox.Game.Signs;

public enum SignAlign : byte
{
    Left,
    Center,
    Right
}

public readonly struct SignRun
{
    public readonly string Text;
    public readonly bool Bold;
    public readonly Color4 Color;

    public SignRun(string text, bool bold, Color4 color)
    {
        Text = text;
        Bold = bold;
        Color = color;
    }
}

public sealed class SignLine
{
    public List<SignRun> Runs { get; } = new List<SignRun>();
    public SignAlign Align { get; set; }
}

public static class SignMarkup
{
    public const int MaxTagLength = 24;
    public const int MaxRawLineLength = 256;

    private static readonly Dictionary<string, Color4> NamedColors = new Dictionary<string, Color4>(StringComparer.OrdinalIgnoreCase)
    {
        ["white"] = new Color4(1f, 1f, 1f, 1f),
        ["black"] = new Color4(0.05f, 0.05f, 0.05f, 1f),
        ["gray"] = new Color4(0.5f, 0.5f, 0.5f, 1f),
        ["grey"] = new Color4(0.5f, 0.5f, 0.5f, 1f),
        ["red"] = new Color4(0.85f, 0.15f, 0.12f, 1f),
        ["green"] = new Color4(0.2f, 0.7f, 0.2f, 1f),
        ["lime"] = new Color4(0.45f, 0.9f, 0.2f, 1f),
        ["blue"] = new Color4(0.2f, 0.35f, 0.9f, 1f),
        ["cyan"] = new Color4(0.25f, 0.8f, 0.9f, 1f),
        ["yellow"] = new Color4(0.95f, 0.85f, 0.15f, 1f),
        ["gold"] = new Color4(1f, 0.77f, 0.16f, 1f),
        ["orange"] = new Color4(1f, 0.55f, 0.1f, 1f),
        ["purple"] = new Color4(0.6f, 0.3f, 0.85f, 1f),
        ["pink"] = new Color4(1f, 0.5f, 0.7f, 1f),
        ["brown"] = new Color4(0.45f, 0.28f, 0.12f, 1f)
    };

    public static IEnumerable<string> ColorNames => NamedColors.Keys;

    public static bool TryParseColor(string value, out Color4 color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        value = value.Trim();
        if (NamedColors.TryGetValue(value, out color)) return true;
        if (value[0] != '#') return false;

        string hex = value.Substring(1);
        if (hex.Length == 3) hex = new string(new[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });
        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)) return false;

        color = new Color4(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        return true;
    }

    public static bool TryParseAlign(string value, out SignAlign align)
    {
        align = SignAlign.Center;
        if (string.IsNullOrWhiteSpace(value)) return false;
        switch (value.Trim().ToLowerInvariant())
        {
            case "left": align = SignAlign.Left; return true;
            case "right": align = SignAlign.Right; return true;
            case "center": align = SignAlign.Center; return true;
            default: return false;
        }
    }

    public static List<SignLine> Parse(string text, Color4 defaultColor, SignAlign defaultAlign)
    {
        var lines = new List<SignLine>();
        var colors = new Stack<Color4>();
        bool bold = false;
        var run = new StringBuilder();

        foreach (var raw in SplitLines(text))
        {
            var line = new SignLine { Align = defaultAlign };
            int i = 0;
            while (i < raw.Length)
            {
                if (TryReadTag(raw, i, out var tag, out int length))
                {
                    Flush(line, run, bold, colors, defaultColor);
                    Apply(tag, line, ref bold, colors);
                    i += length;
                    continue;
                }

                run.Append(raw[i]);
                i++;
            }

            Flush(line, run, bold, colors, defaultColor);
            lines.Add(line);
        }

        return lines;
    }

    public static string Sanitize(string text, int maxLines, int maxLineLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        maxLines = Math.Max(1, maxLines);
        maxLineLength = Math.Max(1, maxLineLength);

        var result = new StringBuilder();
        int lineCount = 0;
        foreach (var raw in SplitLines(Clean(text)))
        {
            if (lineCount == maxLines) break;
            if (lineCount > 0) result.Append('\n');
            AppendLimited(result, raw.Length > MaxRawLineLength ? raw.Substring(0, MaxRawLineLength) : raw, maxLineLength);
            lineCount++;
        }

        return result.ToString().TrimEnd('\n');
    }

    public static int VisibleLength(string line)
    {
        if (string.IsNullOrEmpty(line)) return 0;
        int count = 0;
        int i = 0;
        while (i < line.Length)
        {
            if (TryReadTag(line, i, out _, out int length))
            {
                i += length;
                continue;
            }
            count++;
            i++;
        }
        return count;
    }

    public static SignTextSize ReadTextSize(string text)
    {
        var size = SignTextSize.Default;
        if (string.IsNullOrEmpty(text)) return size;

        int i = 0;
        while (i < text.Length)
        {
            if (TryReadTag(text, i, out var tag, out int length))
            {
                SignTextSize.TryParseTag(tag, ref size);
                i += length;
                continue;
            }
            i++;
        }
        return size;
    }

    public static bool IsTagAt(string text, int index)
    {
        return !string.IsNullOrEmpty(text) && index >= 0 && index < text.Length && TryReadTag(text, index, out _, out _);
    }

    public static string StripTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var result = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            if (TryReadTag(text, i, out _, out int length))
            {
                i += length;
                continue;
            }
            result.Append(text[i]);
            i++;
        }
        return result.ToString();
    }

    private static string Clean(string text)
    {
        var result = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c == '\n') result.Append(c);
            else if (c == '\t') result.Append(' ');
            else if (!char.IsControl(c) && !char.IsSurrogate(c)) result.Append(c);
        }
        return result.ToString();
    }

    private static void AppendLimited(StringBuilder result, string line, int maxVisible)
    {
        int visible = 0;
        int i = 0;
        while (i < line.Length)
        {
            if (TryReadTag(line, i, out _, out int length))
            {
                result.Append(line, i, length);
                i += length;
                continue;
            }

            if (visible < maxVisible) result.Append(line[i]);
            visible++;
            i++;
        }
    }

    private static string[] SplitLines(string text)
    {
        return (text ?? "").Replace("\r", "").Split('\n');
    }

    private static bool TryReadTag(string text, int start, out string tag, out int length)
    {
        tag = null;
        length = 0;
        if (text[start] != '<') return false;

        int end = text.IndexOf('>', start + 1);
        if (end < 0 || end - start - 1 > MaxTagLength) return false;

        string content = text.Substring(start + 1, end - start - 1).Trim().ToLowerInvariant();
        if (!IsKnownTag(content)) return false;

        tag = content;
        length = end - start + 1;
        return true;
    }

    private static bool IsKnownTag(string tag)
    {
        switch (tag)
        {
            case "b":
            case "/b":
            case "/color":
            case "left":
            case "center":
            case "right":
                return true;
        }

        var size = SignTextSize.Default;
        if (SignTextSize.TryParseTag(tag, ref size)) return true;
        return tag.StartsWith("color=", StringComparison.Ordinal) && TryParseColor(tag.Substring(6).Trim('"', '\''), out _);
    }

    private static void Apply(string tag, SignLine line, ref bool bold, Stack<Color4> colors)
    {
        switch (tag)
        {
            case "b": bold = true; return;
            case "/b": bold = false; return;
            case "/color": if (colors.Count > 0) colors.Pop(); return;
            case "left": line.Align = SignAlign.Left; return;
            case "center": line.Align = SignAlign.Center; return;
            case "right": line.Align = SignAlign.Right; return;
        }

        if (!tag.StartsWith("color=", StringComparison.Ordinal)) return;
        if (TryParseColor(tag.Substring(6).Trim('"', '\''), out var color)) colors.Push(color);
    }

    private static void Flush(SignLine line, StringBuilder run, bool bold, Stack<Color4> colors, Color4 defaultColor)
    {
        if (run.Length == 0) return;
        line.Runs.Add(new SignRun(run.ToString(), bold, colors.Count > 0 ? colors.Peek() : defaultColor));
        run.Clear();
    }
}
