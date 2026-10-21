using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;

namespace Spacebox.Game.Mobs;

public static class MobSounds
{
    public const float MinDistance = 2f;
    public const float MaxDistance = 16f;
    public const float Rolloff = 1.6f;
    public const float PitchVariation = 0.1f;

    private static readonly SpatialSoundPool _pool = new SpatialSoundPool(3, MinDistance, MaxDistance, Rolloff);

    public static void Play(MobSoundsJSON sounds, string soundName, Vector3 position, Random random)
    {
        if (Application.IsHeadless || sounds == null || string.IsNullOrEmpty(soundName)) return;
        if (!IsInHearingRange(position, Camera.Main?.PositionWorld)) return;
        if (!GameAssets.Sounds.TryGetValue(soundName, out var clip)) return;

        float pitch = 1f + ((float)random.NextDouble() * 2f - 1f) * PitchVariation;
        _pool.Play(clip, position, pitch, sounds.Volume);
    }

    public static bool IsInHearingRange(Vector3 source, Vector3? listener)
    {
        return listener.HasValue && Vector3.DistanceSquared(source, listener.Value) <= MaxDistance * MaxDistance;
    }

    public static float NextIdleDelay(MobSoundsJSON sounds, Random random)
    {
        float min = MathF.Max(0.5f, MathF.Min(sounds.IdleIntervalMin, sounds.IdleIntervalMax));
        float max = MathF.Max(min, MathF.Max(sounds.IdleIntervalMin, sounds.IdleIntervalMax));
        return min + (float)random.NextDouble() * (max - min);
    }

    public static void Clear()
    {
        _pool.Clear();
    }
}
