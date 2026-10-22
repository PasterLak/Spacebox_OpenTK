namespace Engine.Platform;

public static class GpuSelectionRules
{
    public const string HighPerformancePreference = "GpuPreference=2;";
    public const string PreferenceKey = "GpuPreference=";

    public static string WindowsPreferenceToWrite(string existingValue)
    {
        if (!string.IsNullOrWhiteSpace(existingValue) && existingValue.Contains(PreferenceKey, StringComparison.OrdinalIgnoreCase)) return null;
        if (string.IsNullOrWhiteSpace(existingValue)) return HighPerformancePreference;
        string trimmed = existingValue.Trim();
        return (trimmed.EndsWith(';') ? trimmed : trimmed + ";") + HighPerformancePreference;
    }

    public static bool CanStorePreferenceFor(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath)) return false;
        string name = Path.GetFileNameWithoutExtension(executablePath);
        return !string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyDictionary<string, string> LinuxOffloadVariables(int gpuCount, bool nvidiaDriverLoaded, Func<string, string> currentValue)
    {
        var variables = new Dictionary<string, string>();
        if (gpuCount < 2) return variables;
        if (IsSet(currentValue, "DRI_PRIME") || IsSet(currentValue, "__NV_PRIME_RENDER_OFFLOAD") || IsSet(currentValue, "__GLX_VENDOR_LIBRARY_NAME")) return variables;

        if (nvidiaDriverLoaded)
        {
            variables["__NV_PRIME_RENDER_OFFLOAD"] = "1";
            variables["__GLX_VENDOR_LIBRARY_NAME"] = "nvidia";
        }
        else
        {
            variables["DRI_PRIME"] = "1";
        }
        return variables;
    }

    public static int CountDrmCards(IEnumerable<string> entryNames)
    {
        int count = 0;
        foreach (var name in entryNames)
        {
            if (name.Length > 4 && name.StartsWith("card", StringComparison.Ordinal) && name.AsSpan(4).IndexOfAnyExceptInRange('0', '9') < 0) count++;
        }
        return count;
    }

    private static bool IsSet(Func<string, string> currentValue, string name) => !string.IsNullOrEmpty(currentValue(name));
}
