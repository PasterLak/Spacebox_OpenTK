using Engine.Audio;
using OpenTK.Mathematics;
using Spacebox.Game;

namespace Spacebox.Network.Combat;

public sealed class CombatSounds
{
    private const int SourcesPerSound = 4;

    private readonly Dictionary<string, AudioSource[]> _sources = new Dictionary<string, AudioSource[]>();
    private readonly Dictionary<string, int> _next = new Dictionary<string, int>();
    private readonly Random _random = new Random();

    public void PlayShot(WeaponItem weapon, Vector3 position)
    {
        if (string.IsNullOrEmpty(weapon.ShotSound) || !GameAssets.Sounds.TryGetValue(weapon.ShotSound, out var clip)) return;

        if (!_sources.TryGetValue(weapon.ShotSound, out var sources))
        {
            sources = new AudioSource[SourcesPerSound];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = new AudioSource(clip);
                sources[i].Setup3D(5f, 300f, 1.5f);
            }
            _sources[weapon.ShotSound] = sources;
            _next[weapon.ShotSound] = 0;
        }

        int index = _next[weapon.ShotSound];
        _next[weapon.ShotSound] = (index + 1) % sources.Length;

        var source = sources[index];
        source.Position = position;
        source.Pitch = _random.Next(95, 105) * 0.01f;
        source.Play();
    }

    public void Clear()
    {
        foreach (var sources in _sources.Values)
        {
            for (int i = 0; i < sources.Length; i++) sources[i]?.Dispose();
        }
        _sources.Clear();
        _next.Clear();
    }
}
