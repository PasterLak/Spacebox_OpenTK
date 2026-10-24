using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Automation;

public static class InputInjector
{
    private const int Forever = -1;

    private static readonly Dictionary<Keys, int> _keys = new Dictionary<Keys, int>();
    private static readonly Dictionary<MouseButton, int> _buttons = new Dictionary<MouseButton, int>();
    private static readonly List<Keys> _releasedKeys = new List<Keys>();
    private static readonly List<MouseButton> _releasedButtons = new List<MouseButton>();
    private static readonly Queue<Action<GameWindow>> _steps = new Queue<Action<GameWindow>>();
    private static readonly Queue<char> _chars = new Queue<char>();
    private static readonly List<Keys> _keyBuffer = new List<Keys>();
    private static readonly List<MouseButton> _buttonBuffer = new List<MouseButton>();

    public static bool IsAvailable => OpenTkInputWriter.IsAvailable;
    public static bool IsIdle => _keys.Count == 0 && _buttons.Count == 0 && _steps.Count == 0 && _chars.Count == 0;
    public static IReadOnlyCollection<Keys> HeldKeys => _keys.Keys;
    public static IReadOnlyCollection<MouseButton> HeldButtons => _buttons.Keys;

    public static void HoldKey(Keys key, int frames = Forever) => _keys[key] = frames;

    public static void ReleaseKey(Keys key)
    {
        if (_keys.Remove(key)) _releasedKeys.Add(key);
    }

    public static void HoldButton(MouseButton button, int frames = Forever) => _buttons[button] = frames;

    public static void ReleaseButton(MouseButton button)
    {
        if (_buttons.Remove(button)) _releasedButtons.Add(button);
    }

    public static void MoveMouse(Vector2 position)
    {
        _steps.Enqueue(window => OpenTkInputWriter.SetPosition(window.MouseState, position));
    }

    public static void Click(Vector2 position, MouseButton button)
    {
        MoveMouse(position);
        _steps.Enqueue(window =>
        {
            OpenTkInputWriter.SetPosition(window.MouseState, position);
            OpenTkInputWriter.SetButton(window.MouseState, button, true);
        });
        _steps.Enqueue(window =>
        {
            OpenTkInputWriter.SetPosition(window.MouseState, position);
            OpenTkInputWriter.SetButton(window.MouseState, button, false);
        });
    }

    public static void TypeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (char c in text) _chars.Enqueue(c);
    }

    public static void ReleaseAll()
    {
        _releasedKeys.AddRange(_keys.Keys);
        _releasedButtons.AddRange(_buttons.Keys);
        _keys.Clear();
        _buttons.Clear();
        _steps.Clear();
        _chars.Clear();
    }

    public static void Apply(GameWindow window)
    {
        if (IsIdle && _releasedKeys.Count == 0 && _releasedButtons.Count == 0) return;
        if (!IsAvailable) return;

        foreach (var key in _releasedKeys) OpenTkInputWriter.SetKey(window.KeyboardState, key, false);
        foreach (var button in _releasedButtons) OpenTkInputWriter.SetButton(window.MouseState, button, false);
        _releasedKeys.Clear();
        _releasedButtons.Clear();

        ApplyKeys(window.KeyboardState);
        ApplyButtons(window.MouseState);

        if (_steps.Count > 0) _steps.Dequeue()(window);

        if (_chars.Count > 0)
        {
            while (_chars.Count > 0) Input.Text.AddChar(_chars.Dequeue());
        }
    }

    private static void ApplyKeys(KeyboardState state)
    {
        if (_keys.Count == 0) return;

        _keyBuffer.Clear();
        _keyBuffer.AddRange(_keys.Keys);
        foreach (var key in _keyBuffer)
        {
            int frames = _keys[key];
            OpenTkInputWriter.SetKey(state, key, true);
            if (frames == Forever) continue;
            if (frames <= 1)
            {
                _keys.Remove(key);
                _releasedKeys.Add(key);
            }
            else _keys[key] = frames - 1;
        }
    }

    private static void ApplyButtons(MouseState state)
    {
        if (_buttons.Count == 0) return;

        _buttonBuffer.Clear();
        _buttonBuffer.AddRange(_buttons.Keys);
        foreach (var button in _buttonBuffer)
        {
            int frames = _buttons[button];
            OpenTkInputWriter.SetButton(state, button, true);
            if (frames == Forever) continue;
            if (frames <= 1)
            {
                _buttons.Remove(button);
                _releasedButtons.Add(button);
            }
            else _buttons[button] = frames - 1;
        }
    }
}
