using System.Runtime.InteropServices;
using System.Text;

namespace Engine.Utils;

public static class TerminalConsole
{
    private const int AttachParentProcess = -1;

    public static void Attach(string title)
    {
        if (OperatingSystem.IsWindows()) AttachWindowsConsole();

        try
        {
            System.Console.OutputEncoding = Encoding.UTF8;
            if (!string.IsNullOrEmpty(title) && OperatingSystem.IsWindows()) System.Console.Title = title;
        }
        catch (IOException)
        {
        }

        Debug.MirrorToStandardOutput = true;
    }

    public static void Clear()
    {
        try
        {
            System.Console.Clear();
        }
        catch (IOException)
        {
        }
    }

    private static void AttachWindowsConsole()
    {
        if (GetConsoleWindow() != IntPtr.Zero) return;
        if (!AttachConsole(AttachParentProcess)) AllocConsole();

        var output = new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true };
        System.Console.SetOut(output);
        System.Console.SetError(output);
        System.Console.SetIn(new StreamReader(System.Console.OpenStandardInput()));
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int processId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
}
