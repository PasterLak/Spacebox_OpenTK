using OpenTK.Mathematics;
using Spacebox.Game.GameMath;

namespace Spacebox.Game.Player.Respawn;

public readonly record struct ReconstructorRecord(ReconstructorKey Key, Vector3i Sector, Vector3 Local, int DnaCount, bool IsPowered)
{
    public Vector3 WorldPosition => SpaceMath.Sector.GetSectorPosition(Sector) + Local;

    public ReconstructorCandidate ToCandidate() => new ReconstructorCandidate(Key, WorldPosition, DnaCount > 0, IsPowered);

    public static ReconstructorRecord At(ReconstructorKey key, Vector3 worldPosition, int dnaCount, bool isPowered)
    {
        var sector = SpaceMath.Sector.GetSectorIndex(worldPosition);
        return new ReconstructorRecord(key, sector, worldPosition - SpaceMath.Sector.GetSectorPosition(sector), dnaCount, isPowered);
    }
}
