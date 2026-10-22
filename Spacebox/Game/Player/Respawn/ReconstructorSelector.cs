using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Respawn;

public static class ReconstructorSelector
{
    public const string NoReconstructor = "No DNA Reconstructor built";
    public const string NoDna = "No DNA samples in the DNA Reconstructor";
    public const string NoPower = "DNA Reconstructor has no power";

    public static bool TryFindNearestReady(IReadOnlyList<ReconstructorCandidate> candidates, Vector3 from, IReadOnlySet<ReconstructorKey> excluded, out ReconstructorCandidate nearest)
    {
        nearest = default;
        bool found = false;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (!candidate.IsReady || (excluded != null && excluded.Contains(candidate.Key))) continue;

            float distance = Vector3.DistanceSquared(from, candidate.WorldPosition);
            if (distance >= nearestDistance) continue;

            nearestDistance = distance;
            nearest = candidate;
            found = true;
        }

        return found;
    }

    public static string GetBlocker(IReadOnlyList<ReconstructorCandidate> candidates)
    {
        if (candidates.Count == 0) return NoReconstructor;

        bool anyDna = false;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].IsReady) return null;
            anyDna |= candidates[i].HasDna;
        }

        return anyDna ? NoPower : NoDna;
    }
}
