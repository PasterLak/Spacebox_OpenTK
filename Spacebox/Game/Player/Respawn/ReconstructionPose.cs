using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Respawn;

public static class ReconstructionPose
{
    public static Quaternion Facing(DnaReconstructorBlock reconstructor, Vector3 eyePosition, Quaternion fallback)
    {
        if (reconstructor?.Entity == null) return fallback;

        var direction = reconstructor.WorldCenter - eyePosition;
        if (direction.LengthSquared < 1e-4f) return fallback;

        var up = reconstructor.Entity.DirectionToWorldSpace(Vector3.UnitY);
        return GravityWalker.LookRotation(direction.Normalized(), up);
    }
}
