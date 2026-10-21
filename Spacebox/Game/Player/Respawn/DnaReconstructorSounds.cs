using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Respawn;

public static class DnaReconstructorSounds
{
    public static void PlaySpawn(DnaReconstructorBlock reconstructor)
    {
        if (reconstructor?.Entity == null) return;

        var sounds = reconstructor.Data.Sounds;
        BlockEventSounds.Play(sounds.Spawn, reconstructor.WorldCenter, sounds.Volume);
    }
}
