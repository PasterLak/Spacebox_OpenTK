using System.Collections.Generic;

namespace Engine
{
    public static class UIManager
    {
        private static readonly Stack<HashSet<string>> _stack = new Stack<HashSet<string>>();
        private static readonly HashSet<string> _activeWindows = new HashSet<string>();

        public static bool IsUIMode => _activeWindows.Count > 0;

        public static void Open(params string[] windowNames)
        {
            if (windowNames == null || windowNames.Length == 0) return;

            var newLayer = new HashSet<string>(windowNames);
            _stack.Push(newLayer);

            foreach (var window in windowNames)
            {
                _activeWindows.Add(window);
            }

            UpdateState();
        }

        public static void CloseTop()
        {
            if (_stack.Count == 0) return;

            _stack.Pop();
            _activeWindows.Clear();

            foreach (var layer in _stack)
            {
                foreach (var window in layer)
                {
                    _activeWindows.Add(window);
                }
            }

            UpdateState();
        }

        public static void CloseAll()
        {
            _stack.Clear();
            _activeWindows.Clear();
            UpdateState();
        }

        public static bool IsOpen(string windowName)
        {
            return _activeWindows.Contains(windowName);
        }

        public static bool IsTop(string windowName)
        {
            if (_stack.Count == 0) return false;
            return _stack.Peek().Contains(windowName);
        }

        public static void Toggle(params string[] windowNames)
        {
            if (windowNames == null || windowNames.Length == 0) return;

            if (IsTop(windowNames[0]))
            {
                CloseTop();
            }
            else
            {
                Open(windowNames);
            }
        }

        private static void UpdateState()
        {
            if (IsUIMode)
            {
                Input.ShowCursor();
                InputManager0.Enabled = false; 
            }
            else
            {
                Input.HideCursor();
                InputManager0.Enabled = true; 
            }
        }
    }
}