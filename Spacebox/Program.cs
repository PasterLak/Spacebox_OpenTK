using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Engine.Audio;
using Spacebox.Network;


namespace Spacebox;

public static class Program
{


   // [STAThread]
    private static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        LaunchOptions.Parse(args);
#if AGENT_API
        Agent.AgentHost.Configure(args);
#endif
        Network.Dedicated.ServerRestart.WaitForPreviousProcess(LaunchOptions.WaitForProcessId);

        if (LaunchOptions.IsDedicated)
        {
            Engine.Application.EnableHeadless();
            Engine.Utils.TerminalConsole.Attach("Spacebox Server");
            Engine.Input.CursorCaptureEnabled = false;
        }

        Game.Resource.GameFolderMigration.Run(AppContext.BaseDirectory);

       
        var monitor = Monitors.GetPrimaryMonitor();
        var _audioManager = AudioDevice.Instance; // move!!!!

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
            StartVisible = !LaunchOptions.IsDedicated && !LaunchOptions.IsHidden,
        };
     
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());


        using (var window = new SpaceboxWindow(GameWindowSettings.Default, nativeWindowSettings))
        {
            // window.UpdateFrequency = 999;
            AppDomain.CurrentDomain.ProcessExit += SpaceboxWindow.Shutdown;
            window.Run();
        }

    }
}

