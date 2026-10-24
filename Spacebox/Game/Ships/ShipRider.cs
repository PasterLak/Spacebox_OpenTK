using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;

namespace Spacebox.Game.Ships;

public sealed class ShipRider
{
    private SpaceEntity _entity;
    private Vector3 _localPosition;
    private Quaternion _lastOrientation = Quaternion.Identity;
    private Vector3 _capturedWorldPosition;
    private bool _hasState;

    public SpaceEntity BoardedEntity => _entity;
    public bool IsBoarded => _entity != null;

    public void Carry(LocalAstronaut player)
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

        if (TurnsView(ShipPilot.IsSeated, ShipPilot.IsUsingCamera)) player.SetRotation(delta * player.GetRotation());
        player.InertiaController.Velocity = Vector3.Transform(player.InertiaController.Velocity, delta);
    }

    public static bool TurnsView(bool seated, bool usingCamera) => !seated || usingCamera;

    public void Capture(LocalAstronaut player, ShipConsoleBlock pilotConsole)
    {
        UpdateBoarding(player, pilotConsole);

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

    public void Board(LocalAstronaut player, SpaceEntity entity)
    {
        if (entity == null || entity == _entity) return;
        if (_entity != null) Leave(player);

        _entity = entity;
        _hasState = false;
        if (entity.Ship != null)
            player.InertiaController.Velocity -= entity.Ship.GetPointVelocity(player.Position);
    }

    public void Leave(LocalAstronaut player)
    {
        if (_entity == null) return;

        if (_entity.Ship != null && !_entity.IsDisposed)
            player.InertiaController.Velocity += _entity.Ship.GetPointVelocity(player.Position);

        _entity = null;
        _hasState = false;
    }

    public void Reset()
    {
        _entity = null;
        _hasState = false;
    }

    private void UpdateBoarding(LocalAstronaut player, ShipConsoleBlock pilotConsole)
    {
        if (pilotConsole != null)
        {
            var pilotEntity = pilotConsole.Entity;
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
