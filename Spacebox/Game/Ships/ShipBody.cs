using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Ships;

public sealed class ShipBody
{
    private const float InterpolationDelay = 0.1f;
    private const float MaxSubstepTravel = 0.35f;
    private const int MaxSubsteps = 8;

    private readonly SpaceEntity _entity;
    private readonly ShipSnapshotBuffer _snapshots = new ShipSnapshotBuffer();
    private readonly ShipCollider _collider;
    private readonly ShipMassCache _massCache = new ShipMassCache();
    private int _massVersion = -1;
    private double _clock;

    public ShipMotion Motion { get; } = new ShipMotion();
    public ShipCollider Collider => _collider;
    public SpaceEntity Entity => _entity;
    public Vector3 CenterOfMassLocal => _entity.CenterOfMass - _entity.PositionWorld;
    public bool IsSimulated => !Spacebox.Network.GameSession.IsRemoteClient;

    public event Action<ShipBody> Moved;

    public ShipBody(SpaceEntity entity)
    {
        _entity = entity;
        _collider = new ShipCollider(entity);
    }

    public void ApplyForceAtLocal(Vector3 pointLocal, Vector3 forceLocal)
    {
        if (!IsSimulated) return;

        RefreshMassProperties();
        Motion.ApplyForce(pointLocal, forceLocal, CenterOfMassLocal);
    }

    public void ApplyImpulseAtWorld(Vector3 worldPoint, Vector3 worldImpulse)
    {
        if (!IsSimulated) return;

        RefreshMassProperties();
        var local = _entity.WorldToEntityLocal(worldPoint);
        var impulseLocal = _entity.DirectionToEntitySpace(worldImpulse);
        Motion.ApplyImpulse(local, impulseLocal, CenterOfMassLocal, _entity.Orientation);
    }

    public Vector3 GetPointVelocity(Vector3 worldPoint)
    {
        return Motion.GetPointVelocity(_entity.WorldToEntityLocal(worldPoint), CenterOfMassLocal, _entity.Orientation);
    }

    public void ApplySnapshot(Vector3 position, Quaternion orientation, Vector3 velocity, Vector3 angularVelocity)
    {
        _snapshots.Add(_clock, position, orientation, velocity);
        Motion.Velocity = velocity;
        Motion.AngularVelocity = angularVelocity;
    }

    public void Update(float deltaTime)
    {
        _clock += deltaTime;

        if (!IsSimulated)
        {
            if (_snapshots.TrySample(_clock - InterpolationDelay, out var position, out var sampledOrientation))
                _entity.SetPose(position, sampledOrientation);
            return;
        }

        if (_entity.Chunks.Count == 0) return;
        if (!Motion.IsMoving && !Motion.HasPendingForce) return;

        RefreshMassProperties();

        var settings = ShipPhysicsSettings.Current;
        float step = Math.Min(deltaTime, settings.MaxStep);
        Motion.IntegrateVelocity(step, _entity.Orientation, settings);

        if (_collider.TryFindContact(_entity.EntityLocalToWorld(CenterOfMassLocal), out var overlap))
        {
            _collider.PushOut(overlap);
            ResolveContact(overlap, settings.CollisionBounce);
        }

        int substeps = ShipContactSolver.GetSubsteps(Motion.Velocity.Length, Motion.AngularVelocity.Length, _collider.Radius, step, MaxSubstepTravel, MaxSubsteps);
        float subStep = step / substeps;

        for (int i = 0; i < substeps && Motion.IsMoving; i++)
        {
            var previousPosition = _entity.PositionWorld;
            var previousOrientation = _entity.Orientation;
            var comOffset = CenterOfMassLocal;
            var comWorld = _entity.EntityLocalToWorld(comOffset);
            var orientation = previousOrientation;

            comWorld += Motion.Advance(subStep, ref orientation);
            _entity.SetPose(comWorld - Vector3.Transform(comOffset, orientation), orientation);

            if (!_collider.TryFindContact(comWorld, out var contact)) continue;

            _entity.SetPose(previousPosition, previousOrientation);
            if (!ResolveContact(contact, settings.CollisionBounce))
            {
                _collider.PushOut(contact);
                Motion.AngularVelocity *= 0.5f;
            }
            break;
        }

        if (_entity.Sector != null) _entity.IsModified = true;
        Moved?.Invoke(this);
    }

    private bool ResolveContact(in ShipContact contact, float bounce)
    {
        var other = contact.Other.Ship;
        var normal = contact.NormalWorld;
        var relative = GetPointVelocity(contact.PointWorld) - (other?.GetPointVelocity(contact.PointWorld) ?? Vector3.Zero);

        float inverseA = GetInverseMassAlong(contact.PointWorld, normal);
        float inverseB = other?.GetInverseMassAlong(contact.PointWorld, -normal) ?? 0f;
        if (!ShipContactSolver.TrySolve(relative, normal, inverseA, inverseB, bounce, out var impulse)) return false;

        ApplyImpulseAtWorld(contact.PointWorld, impulse);
        other?.ApplyImpulseAtWorld(contact.PointWorld, -impulse);
        return true;
    }

    public float GetInverseMassAlong(Vector3 worldPoint, Vector3 worldNormal)
    {
        RefreshMassProperties();
        var arm = _entity.WorldToEntityLocal(worldPoint) - CenterOfMassLocal;
        return Motion.GetInverseMassAlong(arm, _entity.DirectionToEntitySpace(worldNormal));
    }

    public void RefreshMassProperties()
    {
        if (_entity.MassVersion == _massVersion) return;
        _massVersion = _entity.MassVersion;

        var properties = _massCache.Compute(_entity.Chunks);
        Motion.SetMassProperties(properties.Mass, properties.GetInertia());
        _collider.RefreshHull(CenterOfMassLocal);
    }
}
