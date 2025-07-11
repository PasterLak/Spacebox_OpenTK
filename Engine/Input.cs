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
        public static MouseState Mouse => _gameWindow.MouseState;


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
                Console.WriteLine("There are already an Input Object!");
            }

        }

        public static bool IsKey(Keys key)
        {

            return _lastState.IsKeyDown(key);

        }
        public static bool IsKeyUp(Keys key)
        {

            return _lastState.IsKeyReleased(key);

        }

        public static bool IsKeyDown(Keys key)
        {

            return _lastState.IsKeyPressed(key);

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

            // MoveCursorToCenter();

        }
        public static void HideCursor()
        {
            SetCursorState(CursorState.Grabbed);
            MoveCursorToCenter();
        }

        public static void MoveCursorToCenter()
        {
            _gameWindow.MousePosition = GetCenter(_gameWindow);
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

        }

        public static CursorState GetCursorState()
        {
            return _gameWindow.CursorState;
        }



    }
}
