namespace Engine.CanvasUI;

public sealed class TextEditState
{
    private const int MaxUndo = 200;

    private readonly List<(string Text, int Caret, int Anchor)> _undo = new List<(string, int, int)>();
    private readonly List<(string Text, int Caret, int Anchor)> _redo = new List<(string, int, int)>();
    private string _text = "";

    public TextEditState(bool multiline = false)
    {
        Multiline = multiline;
    }

    public bool Multiline { get; }
    public int MaxLength { get; set; }
    public int Caret { get; private set; }
    public int Anchor { get; private set; }
    public bool HasSelection => Caret != Anchor;
    public int SelectionStart => Math.Min(Caret, Anchor);
    public int SelectionEnd => Math.Max(Caret, Anchor);
    public string SelectedText => HasSelection ? _text.Substring(SelectionStart, SelectionEnd - SelectionStart) : "";

    public string Text
    {
        get => _text;
        set
        {
            _text = Clean(value);
            Caret = Math.Min(Caret, _text.Length);
            Anchor = Math.Min(Anchor, _text.Length);
            _undo.Clear();
            _redo.Clear();
        }
    }

    public void SetCaret(int position, bool select)
    {
        Caret = Math.Clamp(position, 0, _text.Length);
        if (!select) Anchor = Caret;
    }

    public void SelectAll()
    {
        Anchor = 0;
        Caret = _text.Length;
    }

    public void SelectWordAt(int position)
    {
        position = Math.Clamp(position, 0, _text.Length);
        int start = position;
        int end = position;
        while (start > 0 && IsWordChar(_text[start - 1])) start--;
        while (end < _text.Length && IsWordChar(_text[end])) end++;
        Anchor = start;
        Caret = end;
    }

    public bool Insert(string value)
    {
        value = Clean(value);
        if (value.Length == 0 && !HasSelection) return false;

        int start = SelectionStart;
        int removed = SelectionEnd - start;
        if (MaxLength > 0)
        {
            int room = MaxLength - (_text.Length - removed);
            if (room <= 0 && value.Length > 0) return false;
            if (value.Length > room) value = value.Substring(0, room);
        }

        PushUndo();
        _text = _text.Remove(start, removed).Insert(start, value);
        Caret = Anchor = start + value.Length;
        return true;
    }

    public bool Backspace(bool word)
    {
        if (HasSelection) return DeleteSelection();
        if (Caret == 0) return false;

        int start = word ? WordLeft(Caret) : Caret - 1;
        PushUndo();
        _text = _text.Remove(start, Caret - start);
        Caret = Anchor = start;
        return true;
    }

    public bool Delete(bool word)
    {
        if (HasSelection) return DeleteSelection();
        if (Caret >= _text.Length) return false;

        int end = word ? WordRight(Caret) : Caret + 1;
        PushUndo();
        _text = _text.Remove(Caret, end - Caret);
        Anchor = Caret;
        return true;
    }

    public bool DeleteSelection()
    {
        if (!HasSelection) return false;
        PushUndo();
        int start = SelectionStart;
        _text = _text.Remove(start, SelectionEnd - start);
        Caret = Anchor = start;
        return true;
    }

    public void MoveLeft(bool word, bool select)
    {
        if (!select && HasSelection)
        {
            SetCaret(SelectionStart, false);
            return;
        }
        SetCaret(word ? WordLeft(Caret) : Caret - 1, select);
    }

    public void MoveRight(bool word, bool select)
    {
        if (!select && HasSelection)
        {
            SetCaret(SelectionEnd, false);
            return;
        }
        SetCaret(word ? WordRight(Caret) : Caret + 1, select);
    }

    public void MoveHome(bool select) => SetCaret(Multiline ? LineStart(Caret) : 0, select);

    public void MoveEnd(bool select) => SetCaret(Multiline ? LineEnd(Caret) : _text.Length, select);

    public void MoveDocumentStart(bool select) => SetCaret(0, select);

    public void MoveDocumentEnd(bool select) => SetCaret(_text.Length, select);

    public void MoveLines(int delta, bool select)
    {
        if (!Multiline || delta == 0) return;

        int column = Caret - LineStart(Caret);
        int position = Caret;
        for (int i = 0; i < Math.Abs(delta); i++)
        {
            if (delta < 0)
            {
                int start = LineStart(position);
                if (start == 0) { position = 0; column = 0; break; }
                position = start - 1;
            }
            else
            {
                int end = LineEnd(position);
                if (end >= _text.Length) { position = _text.Length; column = int.MaxValue; break; }
                position = end + 1;
            }
        }

        int lineStart = LineStart(position);
        SetCaret(Math.Min(lineStart + column, LineEnd(position)), select);
    }

    public int LineStart(int position)
    {
        position = Math.Clamp(position, 0, _text.Length);
        int index = position > 0 ? _text.LastIndexOf('\n', position - 1) : -1;
        return index + 1;
    }

    public int LineEnd(int position)
    {
        int index = _text.IndexOf('\n', Math.Clamp(position, 0, _text.Length));
        return index < 0 ? _text.Length : index;
    }

    public (int Line, int Column) LineAndColumn(int position)
    {
        position = Math.Clamp(position, 0, _text.Length);
        int line = 0;
        for (int i = 0; i < position; i++) if (_text[i] == '\n') line++;
        return (line, position - LineStart(position));
    }

    public bool Undo()
    {
        if (_undo.Count == 0) return false;
        _redo.Add((_text, Caret, Anchor));
        Restore(_undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0) return false;
        _undo.Add((_text, Caret, Anchor));
        Restore(_redo[^1]);
        _redo.RemoveAt(_redo.Count - 1);
        return true;
    }

    private void Restore((string Text, int Caret, int Anchor) state)
    {
        _text = state.Text;
        Caret = Math.Min(state.Caret, _text.Length);
        Anchor = Math.Min(state.Anchor, _text.Length);
    }

    private void PushUndo()
    {
        _redo.Clear();
        _undo.Add((_text, Caret, Anchor));
        if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
    }

    private int WordLeft(int position)
    {
        int i = Math.Max(0, position - 1);
        while (i > 0 && char.IsWhiteSpace(_text[i])) i--;
        bool word = i < _text.Length && IsWordChar(_text[i]);
        while (i > 0 && !char.IsWhiteSpace(_text[i - 1]) && IsWordChar(_text[i - 1]) == word) i--;
        return i;
    }

    private int WordRight(int position)
    {
        int i = Math.Min(_text.Length, position);
        if (i >= _text.Length) return _text.Length;
        bool word = IsWordChar(_text[i]);
        while (i < _text.Length && !char.IsWhiteSpace(_text[i]) && IsWordChar(_text[i]) == word) i++;
        while (i < _text.Length && char.IsWhiteSpace(_text[i]) && _text[i] != '\n') i++;
        return i;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private string Clean(string value)
    {
        value ??= "";
        value = value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");
        if (!Multiline) value = value.Replace('\n', ' ');
        return value;
    }
}
