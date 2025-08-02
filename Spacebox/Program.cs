using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Engine.Audio;


namespace Spacebox
{
    public static class Program
    {


       // [STAThread]
        private static void Main()
        {

            var monitor = Monitors.GetPrimaryMonitor();
            var _audioManager = AudioDevice.Instance;
            // string path = "Resources/WindowPosition.txt";
            // var (x, y) = NumberStorage.LoadNumbers(path);

            var nativeWindowSettings = new NativeWindowSettings()
            {

                // ClientSize = new Vector2i(monitor.HorizontalResolution, monitor.VerticalResolution),
                ClientSize = new Vector2i(1280, 720),
                Size =  new Vector2i(1280, 720),
                Location = new Vector2i((int)(monitor.HorizontalResolution / 2f - (1280/2f)), (int)(monitor.VerticalResolution / 2f -(720 / 2f))),
                Title = "Spacebox",
                APIVersion = new Version(3, 3),
                // This is needed to run on macos

               

                Flags = ContextFlags.ForwardCompatible,
            };
         
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());


            using (var window = new Window(GameWindowSettings.Default, nativeWindowSettings))
            {
               // window.UpdateFrequency = 999;
                window.Run();
            }

        }
    }
}

