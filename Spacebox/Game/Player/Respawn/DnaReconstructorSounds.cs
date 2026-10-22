using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player.Respawn;

public static class DnaReconstructorSounds
{
    public static void PlaySpawn(DnaReconstructorBlock reconstructor)
    {
        if (reconstructor?.Entity == null) return;

        PlaySpawn(reconstructor.Data, reconstructor.WorldCenter);
    }

    public static void PlaySpawn(DnaReconstructorBlockData data, Vector3 position)
    {
        if (data == null) return;

        var sounds = data.Sounds;
        BlockEventSounds.Play(sounds.Spawn, position, sounds.Volume);
    }
}
