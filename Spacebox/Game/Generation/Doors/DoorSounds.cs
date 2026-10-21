using OpenTK.Mathematics;
using Spacebox.Game.Effects;

namespace Spacebox.Game.Generation.Doors;

public static class DoorSounds
{
    public const float MinDistance = 2f;
    public const float MaxDistance = 20f;
    public const float Rolloff = 1.4f;
    public const float PitchVariation = 0.04f;

    private static readonly RangedSoundPlayer _player = new RangedSoundPlayer(2, MinDistance, MaxDistance, Rolloff, PitchVariation);

    public static void Play(string soundName, Vector3 position)
    {
        _player.Play(soundName, position);
    }

    public static void Clear()
    {
        _player.Clear();
    }
}
