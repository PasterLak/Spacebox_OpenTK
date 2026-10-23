using Engine;
using Engine.Audio;

namespace Spacebox.Game.GUI.Sounds;

public static class UiSounds
{
    private sealed class Entry
    {
        public AudioSource Source;
        public float LastPlayed = float.MinValue;
    }

    private static readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
    private static bool _enabled;

    public static void Initialize()
    {
        _enabled = !Application.IsHeadless;
    }

    public static void Play(UiSound sound)
    {
        Play(UiSoundCatalog.Get(sound));
    }

    public static void Play(UiSoundSpec spec)
    {
        if (!_enabled || spec.IsEmpty) return;

        var entry = GetEntry(spec.Clip);
        if (entry?.Source == null) return;

        float now = Time.Total;
        if (now >= entry.LastPlayed && now - entry.LastPlayed < spec.MinInterval) return;
        entry.LastPlayed = now;

        if (entry.Source.IsPlaying) entry.Source.Stop();
        entry.Source.Volume = spec.Volume;
        entry.Source.Play();
    }

    public static void Dispose()
    {
        foreach (var entry in _entries.Values) entry.Source?.Dispose();
        _entries.Clear();
        _enabled = false;
    }

    private static Entry GetEntry(string clipName)
    {
        if (_entries.TryGetValue(clipName, out var entry) && entry.Source != null && !entry.Source.IsDisposed) return entry;

        var clip = Resources.Get<AudioClip>(clipName);
        entry = new Entry { Source = clip == null ? null : new AudioSource(clip, AudioChannel.UI) };
        _entries[clipName] = entry;
        return entry;
    }
}
