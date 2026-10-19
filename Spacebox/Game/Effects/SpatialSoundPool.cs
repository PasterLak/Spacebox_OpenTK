using Engine.Audio;
using OpenTK.Mathematics;

namespace Spacebox.Game.Effects;

public sealed class SpatialSoundPool
{
    private sealed class Entry
    {
        public AudioSource[] Sources;
        public int Next;
    }

    private readonly int _sourcesPerClip;
    private readonly float _minDistance;
    private readonly float _maxDistance;
    private readonly float _rolloff;
    private readonly Dictionary<AudioClip, Entry> _entries = new Dictionary<AudioClip, Entry>();

    public SpatialSoundPool(int sourcesPerClip, float minDistance, float maxDistance, float rolloff)
    {
        _sourcesPerClip = Math.Max(1, sourcesPerClip);
        _minDistance = minDistance;
        _maxDistance = maxDistance;
        _rolloff = rolloff;
    }

    public void Play(AudioClip clip, Vector3 position, float pitch = 1f, float volume = 1f)
    {
        if (clip == null) return;

        if (!_entries.TryGetValue(clip, out var entry))
        {
            entry = new Entry { Sources = new AudioSource[_sourcesPerClip] };
            for (int i = 0; i < entry.Sources.Length; i++)
            {
                entry.Sources[i] = new AudioSource(clip);
                entry.Sources[i].Setup3D(_minDistance, _maxDistance, _rolloff);
            }
            _entries[clip] = entry;
        }

        var source = entry.Sources[entry.Next];
        entry.Next = (entry.Next + 1) % entry.Sources.Length;

        if (source.IsPlaying) source.Stop();
        source.Position = position;
        source.Pitch = pitch;
        source.Volume = volume;
        source.Play();
    }

    public void Clear()
    {
        foreach (var entry in _entries.Values)
        {
            for (int i = 0; i < entry.Sources.Length; i++) entry.Sources[i]?.Dispose();
        }
        _entries.Clear();
    }
}
