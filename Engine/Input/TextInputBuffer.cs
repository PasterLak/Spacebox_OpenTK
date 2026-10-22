using System.Text;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine;

public readonly struct KeyPress
{
    public KeyPress(Keys key, bool shift, bool control, bool alt, bool isRepeat)
    {
        Key = key;
        Shift = shift;
        Control = control;
        Alt = alt;
        IsRepeat = isRepeat;
    }

    public Keys Key { get; }
    public bool Shift { get; }
    public bool Control { get; }
    public bool Alt { get; }
    public bool IsRepeat { get; }
}

public sealed class TextInputBuffer
{
    private readonly StringBuilder _pendingText = new StringBuilder();
    private readonly List<KeyPress> _pendingKeys = new List<KeyPress>();
    private readonly List<KeyPress> _frameKeys = new List<KeyPress>();

    public string FrameText { get; private set; } = "";
    public IReadOnlyList<KeyPress> FrameKeys => _frameKeys;

    public void AddChar(char c)
    {
        if (!char.IsControl(c)) _pendingText.Append(c);
    }

    public void AddText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (char c in text) AddChar(c);
    }

    public void AddKey(KeyPress press)
    {
        _pendingKeys.Add(press);
    }

    public void BeginFrame(Func<Keys, bool> pressedThisFrame, bool shift, bool control, bool alt)
    {
        FrameText = _pendingText.ToString();
        _pendingText.Clear();
        _frameKeys.Clear();
        _frameKeys.AddRange(_pendingKeys);
        _pendingKeys.Clear();

        if (pressedThisFrame == null) return;
        foreach (var key in TrackedKeys)
        {
            if (!pressedThisFrame(key) || Contains(key)) continue;
            _frameKeys.Add(new KeyPress(key, shift, control, alt, false));
        }
    }

    private bool Contains(Keys key)
    {
        for (int i = 0; i < _frameKeys.Count; i++)
            if (_frameKeys[i].Key == key && !_frameKeys[i].IsRepeat) return true;
        return false;
    }

    public static readonly Keys[] TrackedKeys =
    {
        Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
        Keys.Backspace, Keys.Delete, Keys.Enter, Keys.KeyPadEnter, Keys.Tab, Keys.Escape,
        Keys.A, Keys.C, Keys.V, Keys.X, Keys.Y, Keys.Z, Keys.S, Keys.F, Keys.D, Keys.F2, Keys.F5
    };
}
