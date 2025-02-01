﻿using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Engine.Audio;
using System.Reflection;


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
                Location = new Vector2i((int)(monitor.HorizontalResolution / 2f), (int)(monitor.VerticalResolution / 2f)),
                Title = "Spacebox",
                APIVersion = new Version(3, 3),
                // This is needed to run on macos

                Flags = ContextFlags.ForwardCompatible,
            };


            using (var window = new Window(GameWindowSettings.Default, nativeWindowSettings))
            {

                window.Run();
            }

        }
    }
}

