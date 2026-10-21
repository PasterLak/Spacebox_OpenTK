using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Respawn;

public static class DnaReconstructorSounds
{
    public const float MinDistance = 2f;
    public const float MaxDistance = 24f;
    public const float Rolloff = 1.4f;
    public const float PitchVariation = 0.03f;

    private static readonly RangedSoundPlayer _player = new RangedSoundPlayer(2, MinDistance, MaxDistance, Rolloff, PitchVariation);

    public static void PlaySpawn(DnaReconstructorBlock reconstructor)
    {
        if (reconstructor?.Entity == null) return;

        var sounds = reconstructor.Data.Sounds;
        _player.Play(sounds.Spawn, reconstructor.WorldCenter, sounds.Volume);
    }

    public static void Clear()
    {
        _player.Clear();
    }
}
