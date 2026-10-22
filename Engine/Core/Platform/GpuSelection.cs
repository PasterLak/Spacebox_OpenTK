using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Engine.Platform;

public static class GpuSelection
{
    private const string WindowsPreferencesKey = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private const string LinuxDrmFolder = "/sys/class/drm";
    private const string LinuxNvidiaDriverFolder = "/proc/driver/nvidia";

    private static IntPtr _nvidiaApi;

    public static void PreferDedicatedGpu()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                LoadNvidiaApi();
                StoreWindowsPreference(Environment.ProcessPath);
            }
            else if (OperatingSystem.IsLinux())
            {
                ApplyLinuxOffload();
            }
        }
        catch (Exception ex)
        {
            Debug.Warning($"[GPU] Could not request the dedicated GPU: {ex.Message}");
        }
    }

    private static void LoadNvidiaApi()
    {
        string library = Environment.Is64BitProcess ? "nvapi64.dll" : "nvapi.dll";
        if (_nvidiaApi == IntPtr.Zero && NativeLibrary.TryLoad(library, out var handle)) _nvidiaApi = handle;
    }

    [SupportedOSPlatform("windows")]
    private static void StoreWindowsPreference(string executablePath)
    {
        if (!GpuSelectionRules.CanStorePreferenceFor(executablePath)) return;

        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(WindowsPreferencesKey);
        if (key == null) return;

        string value = GpuSelectionRules.WindowsPreferenceToWrite(key.GetValue(executablePath) as string);
        if (value == null) return;

        key.SetValue(executablePath, value);
        Debug.Log("[GPU] Windows graphics preference set to high performance for this game");
    }

    private static void ApplyLinuxOffload()
    {
        if (!Directory.Exists(LinuxDrmFolder)) return;

        var names = new List<string>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(LinuxDrmFolder)) names.Add(Path.GetFileName(entry));

        var variables = GpuSelectionRules.LinuxOffloadVariables(GpuSelectionRules.CountDrmCards(names), Directory.Exists(LinuxNvidiaDriverFolder), Environment.GetEnvironmentVariable);
        foreach (var pair in variables) Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        if (variables.Count > 0) Debug.Log("[GPU] Two GPUs found: rendering on the dedicated one (" + string.Join(", ", variables.Keys) + ")");
    }
}
