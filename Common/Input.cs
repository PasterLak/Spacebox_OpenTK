using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace Spacebox.Common
{
    public class Input
    {
        private static Input _instance;
        private static GameWindow _gameWindow;
        private static KeyboardState _lastState;

        public Input() { }

        public static void Initialize(GameWindow gameWindow) 
        { 
            if(_instance == null)
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

        public static void Update()
        {
            _lastState = _gameWindow.KeyboardState;
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

        public static bool IsAnyKeyDown()
        {
            return _lastState.IsAnyKeyDown;
        }

        public static void SetCursorState(CursorState state)
        {
            _gameWindow.CursorState = state;
        }

        public static MouseState MouseState => _gameWindow.MouseState;


    }
}
