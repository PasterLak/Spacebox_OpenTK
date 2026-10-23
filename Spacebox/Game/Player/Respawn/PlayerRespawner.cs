using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player.Respawn;

public sealed class PlayerRespawner
{
    public const string Requesting = "Contacting the DNA Reconstructor...";
    private const float DefaultReconstructionTime = 5f;

    private static readonly IReconstructionChannel LocalChannel = new LocalReconstructionChannel();

    private readonly LocalAstronaut _player;
    private bool _isRequesting;

    public event Action<ReconstructionGrant> Reconstructed;

    public PlayerRespawner(LocalAstronaut player)
    {
        _player = player;
    }

    private static IReconstructionChannel Channel => NetworkManager.Instance.IsRemoteClient ? Spacebox.Network.Players.ReconstructionNetwork.Channel : LocalChannel;

    public string GetReconstructionBlocker()
    {
        return _isRequesting ? Requesting : Channel.GetBlocker(_player.Position);
    }

    public void RespawnAtSpawnPoint()
    {
        _player.Revive(WorldSpawn.For(_player));
        World.Instance?.PreloadAroundPlayer();
        WorldLoadingScreen.Begin(_player, MoveToFreeSpace);
    }

    public bool RespawnAtReconstructor()
    {
        if (_isRequesting) return true;

        _isRequesting = true;
        Channel.Request(_player.Position, OnGrant);
        return true;
    }

    private void OnGrant(ReconstructionGrant? grant)
    {
        _isRequesting = false;
        if (_player.IsAlive) return;

        if (grant is { } value) StartReconstruction(value);
        else RespawnAtSpawnPoint();
    }

    private void StartReconstruction(ReconstructionGrant grant)
    {
        var settings = GameAssets.Blocks.TryGetValue(grant.BlockId, out var data) ? data as DnaReconstructorBlockData : null;
        bool isFar = IsFar(grant);
        var effects = settings != null ? new DnaReconstructionAudio(settings.Sounds) : null;

        GameUi.Current?.DnaReconstruction.Play(settings?.ReconstructionTime ?? DefaultReconstructionTime, () => Arrive(grant, settings, isFar), effects);
    }

    private bool IsFar(ReconstructionGrant grant)
    {
        if (!DnaReconstructorRegistry.TryFind(grant.Key, out _)) return true;
        return Vector3.DistanceSquared(_player.Position, grant.SpawnPosition) > Settings.ENTITY_SEARCH_RADIUS * Settings.ENTITY_SEARCH_RADIUS;
    }

    private void Arrive(ReconstructionGrant grant, DnaReconstructorBlockData settings, bool isFar)
    {
        _player.Revive(grant.SpawnPosition);
        _player.SetRotation(grant.Rotation);

        if (!isFar)
        {
            Settle(grant, settings);
            return;
        }

        World.Instance?.PreloadAroundPlayer();
        WorldLoadingScreen.Begin(_player, () => Settle(grant, settings), () => DnaReconstructorRegistry.TryFind(grant.Key, out _));
    }

    private void Settle(ReconstructionGrant grant, DnaReconstructorBlockData settings)
    {
        if (DnaReconstructorRegistry.TryFind(grant.Key, out var reconstructor) && reconstructor.Entity != null)
        {
            if (SafeSpawnFinder.TryFindNear(reconstructor.Entity, reconstructor.LocalCell, out var position))
            {
                _player.SetPosition(position);
                _player.SetRotation(ReconstructionPose.Facing(reconstructor, position, _player.GetRotation()));
            }
            DnaReconstructorSounds.PlaySpawn(reconstructor);
        }
        else
        {
            DnaReconstructorSounds.PlaySpawn(settings, grant.Center);
        }

        Reconstructed?.Invoke(grant);
    }

    private void MoveToFreeSpace()
    {
        _player.SetPosition(SafeSpawnFinder.FindFreePosition(_player.Position));
    }
}
