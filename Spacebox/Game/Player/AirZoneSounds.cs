using Engine;
using Engine.Audio;

namespace Spacebox.Game.Player;

public static class AirZoneSounds
{
    public const string EnterSound = "airZoneEnter";
    public const string ExitSound = "airZoneExit";
    public const float Volume = 0.6f;
    public const double MinInterval = 0.5;

    private static readonly Dictionary<string, AudioSource> _sources = new Dictionary<string, AudioSource>();
    private static double _lastPlayed = double.MinValue;

    public static void Play(bool enteredAir)
    {
        if (Application.IsHeadless || Time.Total - _lastPlayed < MinInterval) return;

        var source = GetSource(enteredAir ? EnterSound : ExitSound);
        if (source == null) return;

        _lastPlayed = Time.Total;
        if (source.IsPlaying) source.Stop();
        source.Volume = Volume;
        source.Play();
    }

    public static void Clear()
    {
        foreach (var source in _sources.Values) source.Dispose();
        _sources.Clear();
        _lastPlayed = double.MinValue;
    }

    private static AudioSource GetSource(string soundName)
    {
        if (_sources.TryGetValue(soundName, out var source)) return source;
        if (!GameAssets.Sounds.TryGetValue(soundName, out var clip) || clip == null) return null;

        source = new AudioSource(clip, AudioChannel.Effects);
        _sources[soundName] = source;
        return source;
    }
}
