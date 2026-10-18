using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;

namespace Spacebox.Game.Ships;

public static class ShipRider
{
    private static SpaceEntity _entity;
    private static Vector3 _localPosition;
    private static Quaternion _lastOrientation = Quaternion.Identity;
    private static Vector3 _capturedWorldPosition;
    private static bool _hasState;

    public static SpaceEntity BoardedEntity => _entity;
    public static bool IsBoarded => _entity != null;

    public static void Carry(LocalAstronaut player)
    {
        if (_entity == null || !_hasState) return;

        if (_entity.IsDisposed)
        {
            _entity = null;
            _hasState = false;
            return;
        }

        if (player.Position != _capturedWorldPosition)
        {
            _hasState = false;
            return;
        }

        var target = _entity.EntityLocalToWorld(_localPosition);
        if (target != player.Position) player.SetPosition(target);

        var delta = _entity.Orientation * Quaternion.Invert(_lastOrientation);
        if (MathF.Abs(MathF.Abs(delta.W) - 1f) < 1e-7f) return;

        player.SetRotation(delta * player.GetRotation());
        player.InertiaController.Velocity = Vector3.Transform(player.InertiaController.Velocity, delta);
    }

    public static void Capture(LocalAstronaut player)
    {
        UpdateBoarding(player);

        if (_entity == null)
        {
            _hasState = false;
            return;
        }

        _localPosition = _entity.WorldToEntityLocal(player.Position);
        _lastOrientation = _entity.Orientation;
        _capturedWorldPosition = player.Position;
        _hasState = true;
    }

    public static void Board(LocalAstronaut player, SpaceEntity entity)
    {
        if (entity == null || entity == _entity) return;
        if (_entity != null) Leave(player);

        _entity = entity;
        _hasState = false;
        if (entity.Ship != null)
            player.InertiaController.Velocity -= entity.Ship.GetPointVelocity(player.Position);
    }

    public static void Leave(LocalAstronaut player)
    {
        if (_entity == null) return;

        if (_entity.Ship != null && !_entity.IsDisposed)
            player.InertiaController.Velocity += _entity.Ship.GetPointVelocity(player.Position);

        _entity = null;
        _hasState = false;
    }

    public static void Reset()
    {
        _entity = null;
        _hasState = false;
    }

    private static void UpdateBoarding(LocalAstronaut player)
    {
        if (ShipPilot.IsPiloting)
        {
            var pilotEntity = ShipPilot.Console?.Entity;
            if (pilotEntity != null && pilotEntity != _entity) Board(player, pilotEntity);
            return;
        }

        var settings = ShipPhysicsSettings.Current;

        if (_entity != null)
        {
            if (_entity.IsDisposed || !_entity.HasBlockNear(player.Position, (int)MathF.Ceiling(settings.LeaveDistance)))
                Leave(player);
            return;
        }

        var sector = World.CurrentSector;
        if (sector == null || !sector.TryGetNearestEntity(player.Position, out var nearest) || nearest?.Ship == null) return;
        if (nearest.HasBlockNear(player.Position, (int)MathF.Ceiling(settings.BoardDistance))) Board(player, nearest);
    }
}
