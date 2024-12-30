using System.Numerics;

namespace Spacebox.Common
{
    public class WindowManager
    {
        private static WindowManager _instance;
        public static WindowManager Instance => _instance ??= new WindowManager();

        private Dictionary<string, IWindowUI> _windows = new Dictionary<string, IWindowUI>();


        public event Action<bool> OnCursorVisibilityChanged;

        private WindowManager()
        {
        }

        public void RegisterWindow(string name, IWindowUI window)
        {
            if (!_windows.ContainsKey(name))
            {
                _windows.Add(name, window);
                window.OnOpen += () => WindowOpened(name);
                window.OnClose += () => WindowClosed(name);
            }
        }

        public void UnregisterWindow(string name)
        {
            if (_windows.ContainsKey(name))
            {
                _windows.Remove(name);
            }
        }

        public void OpenWindow(string name)
        {
            if (_windows.ContainsKey(name))
            {
                _windows[name].Open();
            }
        }

        public void CloseWindow(string name)
        {
            if (_windows.ContainsKey(name))
            {
                _windows[name].Close();
            }
        }

        public void ToggleWindow(string name)
        {
            if (_windows.ContainsKey(name))
            {
                if (_windows[name].IsVisible)
                    _windows[name].Close();
                else
                    _windows[name].Open();
            }
        }

        private void WindowOpened(string name)
        {
            var window = _windows[name];
            if (window.ShowCursor)
            {
                OnCursorVisibilityChanged?.Invoke(true);
            }
        }

        private void WindowClosed(string name)
        {
            bool anyWindowRequiresCursor = false;
            foreach (var win in _windows.Values)
            {
                if (win.IsVisible && win.ShowCursor)
                {
                    anyWindowRequiresCursor = true;
                    break;
                }
            }

            OnCursorVisibilityChanged?.Invoke(anyWindowRequiresCursor);
        }

        public void RenderAll(Vector2 windowSize)
        {
            foreach (var window in _windows.Values)
            {
                window.Render(windowSize);
            }
        }
    }

    public interface IWindowUI
    {
        bool IsVisible { get; }
        bool ShowCursor { get; }

        event Action OnOpen;
        event Action OnClose;

        void Open();
        void Close();
        void Render(Vector2 windowSize);
    }
}