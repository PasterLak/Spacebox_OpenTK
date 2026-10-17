using Engine;
using Engine.Components;
using Engine.Physics;
using OpenTK.Mathematics;

namespace Spacebox.Game.Physics;

public sealed class SphereBody : Component
{
    private const int MaxSubsteps = 8;

    private readonly PhysicsWorld _physics;
    private BoundingSphere _debugSphere;
    private Vector3 _velocity;

    public float Radius { get; set; }
    public float Bounciness { get; set; } = 0.8f;
    public bool IsKinematic { get; set; }
    public bool IsSolid { get; set; } = true;

    public Vector3 Velocity
    {
        get => _velocity;
        set => _velocity = value;
    }

    public Vector3 Center => Owner.Position;

    public SphereBody(PhysicsWorld physics, float radius)
    {
        _physics = physics;
        Radius = radius;
    }

    public override void OnAttached(Node3D owner)
    {
        base.OnAttached(owner);
        _physics?.Register(this);
    }

    public override void OnDetached()
    {
        _physics?.Unregister(this);
        base.OnDetached();
    }

    public override void OnUpdate()
    {
        if (VisualDebug.Enabled) DrawDebug();
        if (IsKinematic || _physics == null || _velocity == Vector3.Zero) return;

        float distance = _velocity.Length * Time.Delta;
        int steps = Math.Clamp((int)MathF.Ceiling(distance / (Radius * 0.5f)), 1, MaxSubsteps);
        float dt = Time.Delta / steps;

        Vector3 position = Owner.Position;
        for (int i = 0; i < steps; i++)
        {
            MoveAxis(ref position, 0, dt);
            MoveAxis(ref position, 1, dt);
            MoveAxis(ref position, 2, dt);
        }
        Owner.Position = position;
    }

    private void MoveAxis(ref Vector3 position, int axis, float dt)
    {
        float delta = _velocity[axis] * dt;
        if (delta == 0f) return;

        Vector3 next = position;
        next[axis] += delta;

        if (_physics.IsBlocked(next, Radius, this))
        {
            _velocity[axis] = -_velocity[axis] * Bounciness;
            return;
        }

        position = next;
    }

    private void DrawDebug()
    {
        _debugSphere ??= new BoundingSphere(Vector3.Zero, Radius);
        _debugSphere.Center = Center;
        _debugSphere.Radius = Radius;
        VisualDebug.DrawBoundingSphere(_debugSphere, Color4.Yellow);
    }
}
