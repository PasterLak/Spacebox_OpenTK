using Engine;
using Engine.Audio;

namespace Spacebox.Game.Effects;

public sealed class LocalSoundPlayer
{
    private readonly Dictionary<string, AudioSource> _sources = new Dictionary<string, AudioSource>();

    public void Play(string soundName, float volume = 1f, float pitch = 1f)
    {
        if (Application.IsHeadless || string.IsNullOrEmpty(soundName)) return;

        var source = GetSource(soundName);
        if (source == null) return;

        if (source.IsPlaying) source.Stop();
        source.Volume = volume;
        source.Pitch = pitch;
        source.Play();
    }

    public void Clear()
    {
        foreach (var source in _sources.Values) source.Dispose();
        _sources.Clear();
    }

    private AudioSource GetSource(string soundName)
    {
        if (_sources.TryGetValue(soundName, out var source)) return source;
        if (!GameAssets.Sounds.TryGetValue(soundName, out var clip) || clip == null) return null;

        source = new AudioSource(clip, AudioChannel.Effects);
        _sources[soundName] = source;
        return source;
    }
}
