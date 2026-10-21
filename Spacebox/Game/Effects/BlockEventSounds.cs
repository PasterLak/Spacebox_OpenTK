using OpenTK.Mathematics;

namespace Spacebox.Game.Effects;

public static class BlockEventSounds
{
    public const float MinDistance = 2f;
    public const float MaxDistance = 24f;
    public const float Rolloff = 1.4f;
    public const float PitchVariation = 0.03f;

    private static readonly RangedSoundPlayer _player = new RangedSoundPlayer(2, MinDistance, MaxDistance, Rolloff, PitchVariation);

    public static void Play(string soundName, Vector3 position, float volume = 1f)
    {
        _player.Play(soundName, position, volume);
    }

    public static void Clear()
    {
        _player.Clear();
    }
}
