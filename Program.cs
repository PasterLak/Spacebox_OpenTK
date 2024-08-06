using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;


namespace Spacebox
{
    public static class Program
    {
        private static void Main()
        {
            
           var monitor = Monitors.GetPrimaryMonitor();
         
            var nativeWindowSettings = new NativeWindowSettings()
            {

                // ClientSize = new Vector2i(monitor.HorizontalResolution, monitor.VerticalResolution),
                ClientSize = new Vector2i(1280, 720),
              
                Title = "Spacebox",
                // This is needed to run on macos
                Flags = ContextFlags.ForwardCompatible,
            };

            // To create a new window, create a class that extends GameWindow, then call Run() on it.
            using (var window = new Window(GameWindowSettings.Default, nativeWindowSettings))
            {
                
                window.Run();
            }

        }
    }
}

