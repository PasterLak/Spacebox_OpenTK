namespace Engine.Audio;

public static class AudioDeviceSwitchRules
{
    public static bool ShouldReopen(bool defaultDeviceChanged, bool connected) => defaultDeviceChanged || !connected;
}
