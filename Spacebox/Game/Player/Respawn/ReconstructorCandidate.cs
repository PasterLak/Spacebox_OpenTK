using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Respawn;

public readonly record struct ReconstructorCandidate(ReconstructorKey Key, Vector3 WorldPosition, bool HasDna, bool IsPowered)
{
    public bool IsReady => HasDna && IsPowered;
}
