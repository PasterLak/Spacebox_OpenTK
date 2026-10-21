using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Effects;

public sealed class RangedSoundPlayer
{
    private readonly SpatialSoundPool _pool;
    private readonly Random _random = new Random();

    public RangedSoundPlayer(int sourcesPerClip, float minDistance, float maxDistance, float rolloff, float pitchVariation)
    {
        MaxDistance = maxDistance;
        PitchVariation = pitchVariation;
        _pool = new SpatialSoundPool(sourcesPerClip, minDistance, maxDistance, rolloff);
    }

    public float MaxDistance { get; }
    public float PitchVariation { get; }

    public bool IsInHearingRange(Vector3 source, Vector3? listener)
    {
        return listener.HasValue && Vector3.DistanceSquared(source, listener.Value) <= MaxDistance * MaxDistance;
    }

    public void Play(string soundName, Vector3 position, float volume = 1f, Random random = null)
    {
        if (Application.IsHeadless || string.IsNullOrEmpty(soundName)) return;
        if (!IsInHearingRange(position, Camera.Main?.PositionWorld)) return;
        if (!GameAssets.Sounds.TryGetValue(soundName, out var clip)) return;

        var rng = random ?? _random;
        float pitch = 1f + ((float)rng.NextDouble() * 2f - 1f) * PitchVariation;
        _pool.Play(clip, position, pitch, volume);
    }

    public void Clear()
    {
        _pool.Clear();
    }
}
