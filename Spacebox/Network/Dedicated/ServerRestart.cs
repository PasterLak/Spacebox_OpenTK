using System.Diagnostics;
using Engine;
using Debug = Engine.Debug;
using Spacebox.Game.Generation;

namespace Spacebox.Network.Dedicated;

public static class ServerRestart
{
    private const string WaitArgument = "--wait-pid";
    private const int WaitMilliseconds = 30000;
    private static bool _stopping;

    public static void Save(string reason)
    {
        if (NetworkSession.Current != null) NetworkSession.Current.SaveWorld(reason);
        else World.Instance?.Save(false);
    }

    public static void Stop() => Stop(null);

    public static void Stop(Action completed)
    {
        if (_stopping) return;
        _stopping = true;
        Debug.Warning("[Server] Stopping: saving the world");
        FlushPlayersThen(() =>
        {
            try
            {
                Save("Stop");
                NetworkSession.Current?.Dispose();
                Application.Quit();
            }
            finally
            {
                completed?.Invoke();
            }
        });
    }

    public static bool Restart()
    {
        string executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable))
        {
            Debug.Error("[Server] Restart failed: the executable path is unknown");
            return false;
        }

        if (_stopping) return false;
        _stopping = true;
        FlushPlayersThen(() =>
        {
            Save("Restart");
            NetworkSession.Current?.Dispose();

            try
            {
                Process.Start(CreateStartInfo(executable, Environment.GetCommandLineArgs(), Environment.ProcessId));
            }
            catch (Exception ex)
            {
                Debug.Error($"[Server] Restart failed: {ex.Message}");
                _stopping = false;
                return;
            }

            Debug.Warning("[Server] Restarting");
            Application.Quit();
        });
        return true;
    }

    private static void FlushPlayersThen(Action action)
    {
        if (NetworkSession.Current == null) action();
        else NetworkSession.Current.FlushPlayerData(() => EngineWindow._mainThreadActions.Enqueue(action));
    }

    public static ProcessStartInfo CreateStartInfo(string executable, string[] commandLine, int processId)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory
        };

        bool skipFirst = commandLine.Length > 0 && !commandLine[0].StartsWith("--");
        for (int i = skipFirst ? 1 : 0; i < commandLine.Length; i++)
        {
            if (string.Equals(commandLine[i], WaitArgument, StringComparison.OrdinalIgnoreCase))
            {
                i++;
                continue;
            }

            info.ArgumentList.Add(commandLine[i]);
        }

        info.ArgumentList.Add(WaitArgument);
        info.ArgumentList.Add(processId.ToString());
        return info;
    }

    public static void WaitForPreviousProcess(int processId)
    {
        if (processId <= 0) return;

        try
        {
            using var previous = Process.GetProcessById(processId);
            previous.WaitForExit(WaitMilliseconds);
        }
        catch (ArgumentException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
