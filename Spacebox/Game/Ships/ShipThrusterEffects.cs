using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Ships;

public sealed class ShipThrusterEffects : IDisposable
{
    private readonly ThrusterExhaustEmitter _exhaust = new ThrusterExhaustEmitter();

    public void Update(Sector sector, float deltaTime)
    {
        if (sector?.Entities != null)
        {
            EmitAll(sector.Entities, deltaTime);
            EmitAll(sector.Visitors, deltaTime);
        }

        _exhaust.Update();
    }

    public void Render()
    {
        _exhaust.Render();
    }

    public void Dispose()
    {
        _exhaust.Dispose();
    }

    private void EmitAll(IReadOnlyList<SpaceEntity> entities, float deltaTime)
    {
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity.HasShipControl) Emit(entity, deltaTime);
        }
    }

    private void Emit(SpaceEntity entity, float deltaTime)
    {
        var thrusters = entity.ShipControl.Thrusters;
        for (int i = 0; i < thrusters.Count; i++)
        {
            var thruster = thrusters[i];
            if (!thruster.IsFiring || !thruster.IsPowered) continue;
            EmitThruster(entity, thruster, deltaTime);
        }
    }

    private void EmitThruster(SpaceEntity entity, ThrusterBlock thruster, float deltaTime)
    {
        var origin = entity.EntityLocalToWorld(thruster.ExhaustPointLocal);
        var direction = entity.DirectionToWorldSpace(thruster.Frame.Front);
        var carrier = entity.Ship?.GetPointVelocity(origin) ?? Vector3.Zero;
        _exhaust.Emit(origin, direction, carrier, ThrusterExhaustEmitter.NozzleSize(thruster.Frame), deltaTime);
    }
}
