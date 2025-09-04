using Engine.InputPro;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace Engine
{
    public class Input
    {
        private static Input _instance;
        private static GameWindow _gameWindow;
        private static KeyboardState _lastState;
        public static KeyboardState Keyboard => _gameWindow.KeyboardState;
        private static MouseState MouseState => _gameWindow.MouseState;

        private static Vector2 _delta = Vector2.Zero;
        public static class Mouse
        {
            public static Vector2 Position => MouseState.Position;
            public static Vector2 ScrollDelta => MouseState.ScrollDelta;
            public static bool IsButtonDown(MouseButton button) => MouseState.IsButtonDown(button);
            public static bool IsButtonReleased(MouseButton button) => MouseState.IsButtonReleased(button);
            public static bool IsButtonPressed(MouseButton button) => MouseState.IsButtonPressed(button);
            public static bool IsAnyButtonDown => MouseState.IsAnyButtonDown;
            public static Vector2 Delta => _delta;
            public static MouseState State => MouseState;

        }
        public Input() { }

        public static void Initialize(GameWindow gameWindow)
        {
            if (_instance == null)
            {
                _instance = new Input();
                _gameWindow = gameWindow;
                _lastState = gameWindow.KeyboardState;
            }
            else
            {
                Debug.Error("There are already an Input Object!");
            }

        }
        private static bool _ignoreNextDelta = false;
        public static void Update()
        {
            if (_ignoreNextDelta)
            {
                _delta = Vector2.Zero;
                _ignoreNextDelta = false;
            }
            else
            {
                _delta = MouseState.Delta;
            }

        }

        public static bool IsKey(Keys key)
        {

            return _lastState.IsKeyDown(key);

        }
        public static bool IsAction(string name)
        {

            return InputManager.Instance.IsActionHeld(name);
           
        }
        public static bool IsKeyUp(Keys key)
        {

            return _lastState.IsKeyReleased(key);

        }

        public static bool IsActionUp(string name)
        {

            return InputManager.Instance.IsActionReleased(name);

        }

        public static bool IsKeyDown(Keys key)
        {

            return _lastState.IsKeyPressed(key);

        }

        public static bool IsActionDown(string name)
        {

            return InputManager.Instance.IsActionPressed(name);

        }

        public static bool IsMouseButton(MouseButton key)
        {

            return Mouse.IsButtonDown(key);

        }
        public static bool IsMouseButtonUp(MouseButton key)
        {

            return Mouse.IsButtonReleased(key);

        }

        public static bool IsMouseButtonDown(MouseButton key)
        {

            return Mouse.IsButtonPressed(key);

        }

        public static Vector2 MouseScrollDelta => Mouse.ScrollDelta;

        public static bool IsAnyKeyDown()
        {

            return _lastState.IsAnyKeyDown || Mouse.IsAnyButtonDown;
        }

        public static void SetCursorState(CursorState state)
        {
            _gameWindow.CursorState = state;

        }
        public static void HideCursor()
        {
            SetCursorState(CursorState.Grabbed);
        }

        
        private static void MoveCursorToCenter()
        {
            _gameWindow.MousePosition = GetCenter(_gameWindow);
            _ignoreNextDelta = true;
        }

        public static Vector2 GetCenter(GameWindow window)
        {

            int windowWidth = window.Size.X;
            int windowHeight = window.Size.Y;

            int posX = windowWidth / 2;
            int posY = windowHeight / 2;

            return new Vector2i(posX, posY);
        }

        public static void ShowCursor()
        {
           
            SetCursorState(CursorState.Normal);
            _delta = Vector2.Zero;
        }

        public static CursorState GetCursorState()
        {
            return _gameWindow.CursorState;
        }



    }
}
