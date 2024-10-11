using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using Spacebox.Common.Audio;


namespace Spacebox
{
    public static class Program
    {
        private static void Main()
        {
            
           var monitor = Monitors.GetPrimaryMonitor();
            var _audioManager = AudioManager.Instance;
            // string path = "Resources/WindowPosition.txt";
            // var (x, y) = NumberStorage.LoadNumbers(path);

            var nativeWindowSettings = new NativeWindowSettings()
            {

                // ClientSize = new Vector2i(monitor.HorizontalResolution, monitor.VerticalResolution),
                ClientSize = new Vector2i(1280, 720),
                // Location = new Vector2i((int)x, (int)y),
                Title = "Spacebox",
                APIVersion = new Version(3, 3),
                // This is needed to run on macos

                Flags = ContextFlags.ForwardCompatible,
            };


            //GameWindowSettings.Default, nativeWindowSettings
            // To create a new window, create a class that extends GameWindow, then call Run() on it.
            /*using (var window = new Window2())
            {
                
                window.Run();
            }*/

            using (var window = new Window(GameWindowSettings.Default, nativeWindowSettings))
            {

                window.Run();
            }

        }
    }
}

