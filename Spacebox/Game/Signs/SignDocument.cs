using System.Globalization;
using System.Text;
using OpenTK.Mathematics;

namespace Spacebox.Game.Signs;

public readonly struct SignChar
{
    public readonly char Value;
    public readonly bool Bold;
    public readonly Color4? Color;

    public SignChar(char value, bool bold, Color4? color)
    {
        Value = value;
        Bold = bold;
        Color = color;
    }

    public SignChar WithStyle(bool bold, Color4? color) => new SignChar(Value, bold, color);
}

public sealed class SignDocumentLine
{
    public List<SignChar> Chars { get; } = new List<SignChar>();
    public SignAlign Align { get; set; }
}

public readonly struct SignCaret : IComparable<SignCaret>
{
    public readonly int Line;
    public readonly int Column;

    public SignCaret(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public int CompareTo(SignCaret other) => Line != other.Line ? Line.CompareTo(other.Line) : Column.CompareTo(other.Column);
    public override string ToString() => $"{Line}:{Column}";
}

public sealed class SignDocument
{
    private const char EscapedTagStart = '‹';

    private readonly List<SignDocumentLine> _lines = new List<SignDocumentLine>();

    public int MaxLines { get; }
    public int MaxLineLength { get; }
    public SignAlign DefaultAlign { get; }
    public IReadOnlyList<SignDocumentLine> Lines => _lines;
    public SignCaret Caret { get; private set; }
    public SignCaret Anchor { get; private set; }
    public bool TypingBold { get; set; }
    public Color4? TypingColor { get; set; }
    public SignTextSize TextSize { get; set; } = SignTextSize.Default;
    public bool HasSelection => Caret.CompareTo(Anchor) != 0;
    public SignCaret SelectionStart => Caret.CompareTo(Anchor) <= 0 ? Caret : Anchor;
    public SignCaret SelectionEnd => Caret.CompareTo(Anchor) <= 0 ? Anchor : Caret;

    public SignDocument(int maxLines, int maxLineLength, SignAlign defaultAlign)
    {
        MaxLines = Math.Max(1, maxLines);
        MaxLineLength = Math.Max(1, maxLineLength);
        DefaultAlign = defaultAlign;
        _lines.Add(new SignDocumentLine { Align = defaultAlign });
    }

    public static SignDocument FromMarkup(string markup, int maxLines, int maxLineLength, SignAlign defaultAlign)
    {
        var document = new SignDocument(maxLines, maxLineLength, defaultAlign);
        document._lines.Clear();
        document.TextSize = SignMarkup.ReadTextSize(markup);

        var colorMarker = new Color4(0.123f, 0.456f, 0.789f, 1f);
        foreach (var parsed in SignMarkup.Parse(SignMarkup.Sanitize(markup, maxLines, maxLineLength), colorMarker, defaultAlign))
        {
            var line = new SignDocumentLine { Align = parsed.Align };
            foreach (var run in parsed.Runs)
            {
                Color4? color = run.Color == colorMarker ? null : run.Color;
                foreach (char c in run.Text) line.Chars.Add(new SignChar(c, run.Bold, color));
            }
            document._lines.Add(line);
        }

        if (document._lines.Count == 0) document._lines.Add(new SignDocumentLine { Align = defaultAlign });
        var end = new SignCaret(document._lines.Count - 1, document._lines[^1].Chars.Count);
        document.Caret = document.Anchor = end;
        document.SyncTypingStyle();
        return document;
    }

    public string ToMarkup()
    {
        var builder = new StringBuilder();
        builder.Append(TextSize.ToTag());
        for (int l = 0; l < _lines.Count; l++)
        {
            if (l > 0) builder.Append('\n');
            var line = _lines[l];
            if (line.Align != DefaultAlign) builder.Append('<').Append(line.Align.ToString().ToLowerInvariant()).Append('>');

            string plain = PlainLine(line);
            bool bold = false;
            Color4? color = null;
            for (int i = 0; i < line.Chars.Count; i++)
            {
                var c = line.Chars[i];
                if (!SameColor(c.Color, color))
                {
                    if (bold) builder.Append("</b>");
                    if (color.HasValue) builder.Append("</color>");
                    bold = false;
                    color = c.Color;
                    if (color.HasValue) builder.Append("<color=").Append(ToHex(color.Value)).Append('>');
                }
                if (c.Bold != bold)
                {
                    builder.Append(c.Bold ? "<b>" : "</b>");
                    bold = c.Bold;
                }
                builder.Append(c.Value == '<' && SignMarkup.IsTagAt(plain, i) ? EscapedTagStart : c.Value);
            }

            if (bold) builder.Append("</b>");
            if (color.HasValue) builder.Append("</color>");
        }
        return builder.ToString();
    }

    public string PlainText()
    {
        var builder = new StringBuilder();
        for (int l = 0; l < _lines.Count; l++)
        {
            if (l > 0) builder.Append('\n');
            foreach (var c in _lines[l].Chars) builder.Append(c.Value);
        }
        return builder.ToString();
    }

    public string SelectedText()
    {
        if (!HasSelection) return "";
        var builder = new StringBuilder();
        var start = SelectionStart;
        var end = SelectionEnd;
        for (int l = start.Line; l <= end.Line; l++)
        {
            if (l > start.Line) builder.Append('\n');
            var line = _lines[l];
            int from = l == start.Line ? start.Column : 0;
            int to = l == end.Line ? end.Column : line.Chars.Count;
            for (int i = from; i < to; i++) builder.Append(line.Chars[i].Value);
        }
        return builder.ToString();
    }

    public void SetCaret(SignCaret caret, bool extendSelection)
    {
        Caret = Clamp(caret);
        if (!extendSelection) Anchor = Caret;
        SyncTypingStyle();
    }

    public void SelectAll()
    {
        Anchor = new SignCaret(0, 0);
        Caret = new SignCaret(_lines.Count - 1, _lines[^1].Chars.Count);
    }

    public void MoveLeft(bool extend)
    {
        if (!extend && HasSelection) { SetCaret(SelectionStart, false); return; }
        var c = Caret;
        c = c.Column > 0 ? new SignCaret(c.Line, c.Column - 1) : c.Line > 0 ? new SignCaret(c.Line - 1, _lines[c.Line - 1].Chars.Count) : c;
        SetCaret(c, extend);
    }

    public void MoveRight(bool extend)
    {
        if (!extend && HasSelection) { SetCaret(SelectionEnd, false); return; }
        var c = Caret;
        c = c.Column < _lines[c.Line].Chars.Count ? new SignCaret(c.Line, c.Column + 1) : c.Line < _lines.Count - 1 ? new SignCaret(c.Line + 1, 0) : c;
        SetCaret(c, extend);
    }

    public void MoveUp(bool extend) => SetCaret(Caret.Line > 0 ? new SignCaret(Caret.Line - 1, Caret.Column) : new SignCaret(0, 0), extend);

    public void MoveDown(bool extend) => SetCaret(Caret.Line < _lines.Count - 1 ? new SignCaret(Caret.Line + 1, Caret.Column) : new SignCaret(Caret.Line, _lines[Caret.Line].Chars.Count), extend);

    public void MoveHome(bool extend) => SetCaret(new SignCaret(Caret.Line, 0), extend);

    public void MoveEnd(bool extend) => SetCaret(new SignCaret(Caret.Line, _lines[Caret.Line].Chars.Count), extend);

    public bool Insert(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        bool changed = DeleteSelection();
        foreach (char raw in text.Replace("\r", ""))
        {
            if (raw == '\n') changed |= NewLine();
            else if (raw == '\t') changed |= InsertChar(' ');
            else if (!char.IsControl(raw) && !char.IsSurrogate(raw)) changed |= InsertChar(raw);
        }
        return changed;
    }

    public bool NewLine()
    {
        bool changed = DeleteSelection();
        if (_lines.Count >= MaxLines) return changed;

        var line = _lines[Caret.Line];
        var next = new SignDocumentLine { Align = line.Align };
        next.Chars.AddRange(line.Chars.GetRange(Caret.Column, line.Chars.Count - Caret.Column));
        line.Chars.RemoveRange(Caret.Column, line.Chars.Count - Caret.Column);
        _lines.Insert(Caret.Line + 1, next);
        SetCaretKeepStyle(new SignCaret(Caret.Line + 1, 0));
        return true;
    }

    public bool Backspace()
    {
        if (DeleteSelection()) return true;
        var c = Caret;
        if (c.Column > 0)
        {
            _lines[c.Line].Chars.RemoveAt(c.Column - 1);
            SetCaret(new SignCaret(c.Line, c.Column - 1), false);
            return true;
        }
        if (c.Line == 0) return false;
        return JoinWithPrevious(c.Line);
    }

    public bool Delete()
    {
        if (DeleteSelection()) return true;
        var c = Caret;
        if (c.Column < _lines[c.Line].Chars.Count)
        {
            _lines[c.Line].Chars.RemoveAt(c.Column);
            return true;
        }
        if (c.Line >= _lines.Count - 1) return false;
        return JoinWithPrevious(c.Line + 1);
    }

    public void ToggleBold()
    {
        if (!HasSelection)
        {
            TypingBold = !TypingBold;
            return;
        }

        bool allBold = true;
        ForEachSelected((line, index) => allBold &= line.Chars[index].Bold);
        ForEachSelected((line, index) => line.Chars[index] = line.Chars[index].WithStyle(!allBold, line.Chars[index].Color));
        TypingBold = !allBold;
    }

    public void ApplyColor(Color4? color)
    {
        TypingColor = color;
        if (!HasSelection) return;
        ForEachSelected((line, index) => line.Chars[index] = line.Chars[index].WithStyle(line.Chars[index].Bold, color));
    }

    public void SetAlign(SignAlign align)
    {
        for (int l = SelectionStart.Line; l <= SelectionEnd.Line; l++) _lines[l].Align = align;
    }

    public void ResetStyle()
    {
        bool all = !HasSelection;
        int firstLine = all ? 0 : SelectionStart.Line;
        int lastLine = all ? _lines.Count - 1 : SelectionEnd.Line;

        if (all)
        {
            foreach (var line in _lines)
                for (int i = 0; i < line.Chars.Count; i++) line.Chars[i] = line.Chars[i].WithStyle(false, null);
        }
        else
        {
            ForEachSelected((line, index) => line.Chars[index] = line.Chars[index].WithStyle(false, null));
        }

        for (int l = firstLine; l <= lastLine; l++) _lines[l].Align = DefaultAlign;
        if (all) TextSize = SignTextSize.Default;
        TypingBold = false;
        TypingColor = null;
    }

    public SignAlign CurrentAlign => _lines[Caret.Line].Align;

    private bool InsertChar(char value)
    {
        var line = _lines[Caret.Line];
        if (line.Chars.Count >= MaxLineLength) return false;
        line.Chars.Insert(Caret.Column, new SignChar(value, TypingBold, TypingColor));
        SetCaretKeepStyle(new SignCaret(Caret.Line, Caret.Column + 1));
        return true;
    }

    private bool JoinWithPrevious(int lineIndex)
    {
        var previous = _lines[lineIndex - 1];
        var line = _lines[lineIndex];
        int column = previous.Chars.Count;
        int room = Math.Max(0, MaxLineLength - previous.Chars.Count);
        previous.Chars.AddRange(line.Chars.Take(room));
        _lines.RemoveAt(lineIndex);
        SetCaret(new SignCaret(lineIndex - 1, column), false);
        return true;
    }

    private bool DeleteSelection()
    {
        if (!HasSelection) return false;
        var start = SelectionStart;
        var end = SelectionEnd;

        var first = _lines[start.Line];
        var last = _lines[end.Line];
        var tail = last.Chars.GetRange(end.Column, last.Chars.Count - end.Column);
        first.Chars.RemoveRange(start.Column, first.Chars.Count - start.Column);
        first.Chars.AddRange(tail.Take(Math.Max(0, MaxLineLength - first.Chars.Count)));
        if (end.Line > start.Line) _lines.RemoveRange(start.Line + 1, end.Line - start.Line);

        SetCaretKeepStyle(start);
        Anchor = Caret;
        return true;
    }

    private void ForEachSelected(Action<SignDocumentLine, int> action)
    {
        var start = SelectionStart;
        var end = SelectionEnd;
        for (int l = start.Line; l <= end.Line; l++)
        {
            var line = _lines[l];
            int from = l == start.Line ? start.Column : 0;
            int to = l == end.Line ? end.Column : line.Chars.Count;
            for (int i = from; i < to; i++) action(line, i);
        }
    }

    private void SetCaretKeepStyle(SignCaret caret)
    {
        Caret = Anchor = Clamp(caret);
    }

    private void SyncTypingStyle()
    {
        var line = _lines[Caret.Line];
        int index = Caret.Column > 0 ? Caret.Column - 1 : 0;
        if (index >= line.Chars.Count) return;
        TypingBold = line.Chars[index].Bold;
        TypingColor = line.Chars[index].Color;
    }

    private SignCaret Clamp(SignCaret caret)
    {
        int line = Math.Clamp(caret.Line, 0, _lines.Count - 1);
        return new SignCaret(line, Math.Clamp(caret.Column, 0, _lines[line].Chars.Count));
    }

    private static string PlainLine(SignDocumentLine line)
    {
        var builder = new StringBuilder(line.Chars.Count);
        foreach (var c in line.Chars) builder.Append(c.Value);
        return builder.ToString();
    }

    private static bool SameColor(Color4? a, Color4? b)
    {
        if (a.HasValue != b.HasValue) return false;
        return !a.HasValue || a.Value == b.Value;
    }

    private static string ToHex(Color4 color)
    {
        int r = (int)MathF.Round(Math.Clamp(color.R, 0f, 1f) * 255f);
        int g = (int)MathF.Round(Math.Clamp(color.G, 0f, 1f) * 255f);
        int b = (int)MathF.Round(Math.Clamp(color.B, 0f, 1f) * 255f);
        return "#" + r.ToString("x2", CultureInfo.InvariantCulture) + g.ToString("x2", CultureInfo.InvariantCulture) + b.ToString("x2", CultureInfo.InvariantCulture);
    }
}
