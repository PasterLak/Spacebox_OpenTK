using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Player.Respawn;

public sealed class PlayerRespawner
{
    private readonly LocalAstronaut _player;

    public event Action<DnaReconstructorBlock> Reconstructed;

    public PlayerRespawner(LocalAstronaut player)
    {
        _player = player;
    }

    public string GetReconstructionBlocker()
    {
        if (DnaReconstructorRegistry.TryFindNearestReady(_player.Position, out _)) return null;
        if (!DnaReconstructorRegistry.HasAny()) return "No DNA Reconstructor nearby";
        if (!DnaReconstructorRegistry.HasAnyWithDna()) return "No DNA samples in the DNA Reconstructor";
        return "DNA Reconstructor has no power";
    }

    public void RespawnAtSpawnPoint()
    {
        _player.Revive(WorldSpawn.For(_player));
        World.Instance?.PreloadAroundPlayer();
        WorldLoadingScreen.Begin(_player, MoveToFreeSpace);
    }

    public bool RespawnAtReconstructor()
    {
        if (!DnaReconstructorRegistry.TryFindNearestReady(_player.Position, out var reconstructor)) return false;
        if (!SafeSpawnFinder.TryFindNear(reconstructor.Entity, reconstructor.LocalCell, out var position)) return false;

        if (Engine.Networking.NetworkManager.Instance.IsRemoteClient)
        {
            var key = new Spacebox.Network.Messages.ContainerKey(reconstructor.Entity.EntityID, reconstructor.LocalCell);
            Spacebox.Network.Containers.ContainerNetwork.RequestTake(key, reconstructor.Data.DnaItem, 1, granted =>
            {
                if (granted) StartReconstruction(reconstructor, position);
                else RespawnAtSpawnPoint();
            });
            return true;
        }

        if (!reconstructor.TryConsumeDna()) return false;

        StartReconstruction(reconstructor, position);
        return true;
    }

    private void StartReconstruction(DnaReconstructorBlock reconstructor, Vector3 position)
    {
        var rotation = FaceTowards(reconstructor, position);
        DnaReconstructionScreen.Play(reconstructor.Data.ReconstructionTime, () =>
        {
            _player.Revive(position);
            _player.SetRotation(rotation);
            DnaReconstructorSounds.PlaySpawn(reconstructor);
            Reconstructed?.Invoke(reconstructor);
        }, new DnaReconstructionAudio(reconstructor.Data.Sounds));
    }

    private void MoveToFreeSpace()
    {
        _player.SetPosition(SafeSpawnFinder.FindFreePosition(_player.Position));
    }

    private Quaternion FaceTowards(DnaReconstructorBlock reconstructor, Vector3 position)
    {
        var direction = reconstructor.WorldCenter - position;
        if (direction.LengthSquared < 1e-4f) return _player.GetRotation();

        var up = reconstructor.Entity.DirectionToWorldSpace(Vector3.UnitY);
        return GravityWalker.LookRotation(direction.Normalized(), up);
    }
}
