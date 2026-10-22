using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Respawn;

public static class DnaReconstructionService
{
    private const int MaxAttempts = 8;
    private const float EntityLoadTimeout = 20f;
    private const float KeepLoadedSeconds = 20f;

    public static void Request(Vector3 deathPosition, int peer, Action<ReconstructionGrant?> done)
    {
        new Attempt(deathPosition, peer, done).Next();
    }

    private sealed class Attempt
    {
        private readonly Vector3 _position;
        private readonly int _peer;
        private readonly Action<ReconstructionGrant?> _done;
        private readonly HashSet<ReconstructorKey> _excluded = new HashSet<ReconstructorKey>();
        private int _attempts;

        public Attempt(Vector3 position, int peer, Action<ReconstructionGrant?> done)
        {
            _position = position;
            _peer = peer;
            _done = done;
        }

        public void Next()
        {
            if (++_attempts > MaxAttempts || !DnaReconstructorRegistry.TryFindNearestReady(_position, _excluded, out var candidate))
            {
                _done(null);
                return;
            }

            if (DnaReconstructorRegistry.TryFind(candidate.Key, out var loaded))
            {
                Use(candidate.Key, loaded);
                return;
            }

            Debug.Log($"[Reconstruction] Loading entity {candidate.Key.EntityId} for the DNA reconstructor at {candidate.WorldPosition}");
            WorldObservers.AddAnchor(candidate.WorldPosition, EntityLoadTimeout + KeepLoadedSeconds);
            EntityLoadRequests.Load(World.Instance, candidate.Key.EntityId, EntityLoadTimeout, (result, _) => OnEntityLoaded(candidate.Key, result));
        }

        private void OnEntityLoaded(ReconstructorKey key, EntityLoadResult result)
        {
            switch (result)
            {
                case EntityLoadResult.Missing:
                    Debug.Warning($"[Reconstruction] Entity {key.EntityId} no longer exists, its DNA reconstructors are forgotten");
                    DnaReconstructorRegistry.ForgetEntity(key.EntityId);
                    break;
                case EntityLoadResult.TimedOut:
                    Debug.Warning($"[Reconstruction] Entity {key.EntityId} did not load in time");
                    _excluded.Add(key);
                    break;
                default:
                    if (DnaReconstructorRegistry.TryFind(key, out var block))
                    {
                        Use(key, block);
                        return;
                    }
                    DnaReconstructorRegistry.Forget(key);
                    break;
            }

            Next();
        }

        private void Use(ReconstructorKey key, DnaReconstructorBlock block)
        {
            if (!CanUse(key, block, out var position) || !block.TryConsumeDna())
            {
                _excluded.Add(key);
                Next();
                return;
            }

            WorldObservers.AddAnchor(block.WorldCenter, block.Data.ReconstructionTime + KeepLoadedSeconds);
            var rotation = ReconstructionPose.Facing(block, position, Quaternion.Identity);
            _done(new ReconstructionGrant(key, position, rotation, block.Id, block.WorldCenter));
        }

        private bool CanUse(ReconstructorKey key, DnaReconstructorBlock block, out Vector3 position)
        {
            position = default;
            if (!block.IsReady) return false;
            if (_peer != NetworkManager.Instance.LocalPeerId && Spacebox.Network.ProtectionNetwork.BlocksPeer(_peer, block.Entity, key.Cell)) return false;
            return SafeSpawnFinder.TryFindNear(block.Entity, block.LocalCell, out position);
        }
    }
}
