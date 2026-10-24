namespace Engine.Audio;

public static class AudioOutputChoice
{
    public static string Resolve(string preferred, IReadOnlyList<string> available)
    {
        if (string.IsNullOrWhiteSpace(preferred) || available == null) return null;
        for (int i = 0; i < available.Count; i++)
            if (string.Equals(available[i], preferred, StringComparison.Ordinal)) return preferred;
        return null;
    }

    public static bool NeedsReopen(string desired, string active, bool defaultChanged, bool connected)
    {
        if (desired != null) return !string.Equals(desired, active, StringComparison.Ordinal) || !connected;
        return active != null || AudioDeviceSwitchRules.ShouldReopen(defaultChanged, connected);
    }
}
