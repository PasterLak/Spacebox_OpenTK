using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Player;

public sealed class PlayerStatsTracker
{
    public const float ExplorationInterval = 1f;
    public const float AsteroidDiscoveryRange = 64f;

    private readonly LocalAstronaut _player;
    private readonly DistanceTracker _distance = new DistanceTracker();
    private float _explorationTimer;

    public PlayerStatsTracker(LocalAstronaut player)
    {
        _player = player;
    }

    public void ResetPosition()
    {
        _distance.Reset(_player.Position);
    }

    public void Update(float deltaTime)
    {
        var stats = _player.PlayerStatistics;
        if (stats == null) return;

        stats.AddPlayTime(deltaTime);
        TrackMovement(stats, deltaTime);

        _explorationTimer -= deltaTime;
        if (_explorationTimer > 0f) return;

        _explorationTimer = ExplorationInterval;
        if (CanTrackMovement()) TrackExploration(stats);
    }

    private bool CanTrackMovement()
    {
        return _player.IsAlive && !_player.IsFrozen && _player.GameMode != GameMode.Spectator && !ShipPilot.IsUsingCamera;
    }

    private void TrackMovement(PlayerStatistics stats, float deltaTime)
    {
        if (!CanTrackMovement())
        {
            _distance.Invalidate();
            return;
        }

        double meters = _distance.Step(_player.Position, deltaTime);
        if (meters <= 0) return;

        stats.AddDistance(meters, ShipPilot.IsPiloting || ShipRider.IsBoarded);
        stats.RecordSpeed(_distance.LastSpeed);
    }

    private void TrackExploration(PlayerStatistics stats)
    {
        var sector = World.CurrentSector;
        if (sector == null) return;

        var index = sector.PositionIndex;
        stats.RecordSector($"{index.X},{index.Y},{index.Z}");

        if (!sector.TryGetNearestEntity(_player.Position, out var entity) || entity is not Asteroid || entity.Chunks.Count == 0) return;

        float distanceSquared = SpaceMath.World.DistanceToBox(entity.ToEntitySpace(_player.Position), entity.GeometryBoundingBox);
        if (distanceSquared <= AsteroidDiscoveryRange * AsteroidDiscoveryRange)
            stats.RecordAsteroid(entity.EntityID);
    }
}
