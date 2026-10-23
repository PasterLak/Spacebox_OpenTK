namespace Spacebox.Game.GUI.Sounds;

public static class UiSoundCatalog
{
    public const string Folder = "Resources/Audio/UI/";

    private static readonly UiSoundSpec[] _specs = Build();

    public static int Count => _specs.Length;

    public static UiSoundSpec Get(UiSound sound)
    {
        int index = (int)sound;
        return index >= 0 && index < _specs.Length ? _specs[index] : default;
    }

    private static UiSoundSpec[] Build()
    {
        var specs = new UiSoundSpec[Enum.GetValues<UiSound>().Length];
        SetClip(specs, UiSound.WindowOpen, "openBlock1", 1f, 0.08f);
        SetClip(specs, UiSound.WindowClose, "openBlock4", 1f, 0.08f);
        Set(specs, UiSound.ButtonHover, "buttonHover.wav", 0.6f, 0.05f);
        SetClip(specs, UiSound.TabSwitch, "Resources/Audio/scroll", 1f, 0.05f);
        SetClip(specs, UiSound.CraftComplete, "click1", 0.6f, 0.08f);
        Set(specs, UiSound.Denied, "denied.wav", 0.7f, 0.25f);
        Set(specs, UiSound.NoPower, "noPower.wav", 0.7f, 0.5f);
        Set(specs, UiSound.WaypointAdded, "waypointAdded.wav", 0.8f, 0.2f);
        Set(specs, UiSound.LowOxygen, "lowOxygen.wav", 0.5f, 0.5f);
        return specs;
    }

    private static void SetClip(UiSoundSpec[] specs, UiSound sound, string clip, float volume, float minInterval)
    {
        specs[(int)sound] = new UiSoundSpec(clip, volume, minInterval);
    }

    private static void Set(UiSoundSpec[] specs, UiSound sound, string file, float volume, float minInterval)
    {
        specs[(int)sound] = new UiSoundSpec(Folder + file, volume, minInterval);
    }
}
