namespace Spacebox.Game.GUI;

public sealed class ChatHistory
{
    private readonly List<string> _lines = new List<string>();
    private readonly int _capacity;
    private int _cursor = -1;
    private string _draft = "";

    public ChatHistory(int capacity = 50)
    {
        _capacity = Math.Max(1, capacity);
    }

    public int Count => _lines.Count;

    public void Add(string line)
    {
        ResetCursor();
        if (string.IsNullOrWhiteSpace(line)) return;
        if (_lines.Count > 0 && _lines[^1] == line) return;

        _lines.Add(line);
        if (_lines.Count > _capacity) _lines.RemoveAt(0);
    }

    public string Older(string current)
    {
        if (_lines.Count == 0) return current;
        if (_cursor < 0)
        {
            _draft = current;
            _cursor = _lines.Count;
        }

        _cursor = Math.Max(0, _cursor - 1);
        return _lines[_cursor];
    }

    public string Newer(string current)
    {
        if (_cursor < 0) return current;

        _cursor++;
        if (_cursor < _lines.Count) return _lines[_cursor];

        string draft = _draft;
        ResetCursor();
        return draft;
    }

    public void ResetCursor()
    {
        _cursor = -1;
        _draft = "";
    }
}
