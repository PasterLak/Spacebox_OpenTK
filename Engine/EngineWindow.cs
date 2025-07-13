

using OpenTK.Windowing.Desktop;
using System.Collections.Concurrent;

namespace Engine
{
    public class EngineWindow : GameWindow
    {

        public static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

      

        public EngineWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
           
        }
    }
}
