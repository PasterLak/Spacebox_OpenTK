namespace Engine.Audio;

public static class AudioDeviceNames
{
    private static readonly string[] Prefixes = { "OpenAL Soft on ", "Generic Software on ", "Generic Hardware on " };

    public static string Display(string device)
    {
        if (string.IsNullOrWhiteSpace(device)) return device;
        for (int i = 0; i < Prefixes.Length; i++)
        {
            if (device.StartsWith(Prefixes[i], StringComparison.OrdinalIgnoreCase) && device.Length > Prefixes[i].Length)
                return device.Substring(Prefixes[i].Length);
        }
        return device;
    }
}
