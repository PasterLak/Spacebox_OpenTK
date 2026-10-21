using OpenTK.Mathematics;
using Spacebox.Game.Effects;

namespace Spacebox.Game.Mobs;

public static class MobSounds
{
    public const float MinDistance = 2f;
    public const float MaxDistance = 16f;
    public const float Rolloff = 1.6f;
    public const float PitchVariation = 0.1f;

    private static readonly RangedSoundPlayer _player = new RangedSoundPlayer(3, MinDistance, MaxDistance, Rolloff, PitchVariation);

    public static void Play(MobSoundsJSON sounds, string soundName, Vector3 position, Random random)
    {
        if (sounds == null) return;
        _player.Play(soundName, position, sounds.Volume, random);
    }

    public static bool IsInHearingRange(Vector3 source, Vector3? listener) => _player.IsInHearingRange(source, listener);

    public static float NextIdleDelay(MobSoundsJSON sounds, Random random)
    {
        float min = MathF.Max(0.5f, MathF.Min(sounds.IdleIntervalMin, sounds.IdleIntervalMax));
        float max = MathF.Max(min, MathF.Max(sounds.IdleIntervalMin, sounds.IdleIntervalMax));
        return min + (float)random.NextDouble() * (max - min);
    }

    public static void Clear()
    {
        _player.Clear();
    }
}
