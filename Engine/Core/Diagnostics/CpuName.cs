using System.Diagnostics;

namespace Engine.Diagnostics;

public static class CpuName
{
    public static string Detect()
    {
        try
        {
            if (OperatingSystem.IsWindows()) return FromWindowsRegistry();
            if (OperatingSystem.IsLinux() && File.Exists("/proc/cpuinfo")) return FromProcCpuInfo(File.ReadAllText("/proc/cpuinfo"));
            if (OperatingSystem.IsMacOS()) return FromCommand("sysctl", "-n machdep.cpu.brand_string");
        }
        catch (Exception ex)
        {
            Debug.Warning($"[System] CPU name not available: {ex.Message}");
        }
        return "";
    }

    public static string FromProcCpuInfo(string text)
    {
        foreach (var line in (text ?? "").Split('\n'))
        {
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;
            string key = line.Substring(0, colon).Trim();
            if (key == "model name" || key == "Model" || key == "Hardware") return Clean(line.Substring(colon + 1));
        }
        return "";
    }

    public static string Clean(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        return string.Join(' ', name.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string FromWindowsRegistry()
    {
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        return Clean(key?.GetValue("ProcessorNameString") as string);
    }

    private static string FromCommand(string file, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(file, arguments) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
        if (process == null) return "";
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(2000);
        return Clean(output);
    }
}
