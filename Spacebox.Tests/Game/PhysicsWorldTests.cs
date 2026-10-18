using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Physics;

namespace Spacebox.Tests;

public class PhysicsWorldTests
{
    private static SphereBody AddBody(PhysicsWorld physics, Vector3 position, float radius)
    {
        var node = new Node3D(position);
        return node.AttachComponent(new SphereBody(physics, radius));
    }

    [Fact]
    public void AttachAndDetach_RegisterBody()
    {
        var physics = new PhysicsWorld(null!);
        var body = AddBody(physics, Vector3.Zero, 1);

        Assert.Contains(body, physics.Bodies);

        body.Owner.DetachComponent(body);

        Assert.Empty(physics.Bodies);
    }

    [Fact]
    public void Raycast_ReturnsClosestBody()
    {
        var physics = new PhysicsWorld(null!);
        var far = AddBody(physics, new Vector3(20, 0, 0), 1);
        var near = AddBody(physics, new Vector3(10, 0, 0), 1);

        Assert.True(physics.Raycast(new Ray(Vector3.Zero, Vector3.UnitX, 100), null!, out var hit));

        Assert.Same(near, hit.Body);
        Assert.Equal(9f, hit.Distance, 4);
        Assert.Equal(9f, hit.Point.X, 4);
        Assert.NotSame(far, hit.Body);
    }

    [Fact]
    public void Raycast_SkipsIgnoredAndDisabledBodies()
    {
        var physics = new PhysicsWorld(null!);
        var shooter = AddBody(physics, Vector3.Zero, 1);
        var disabled = AddBody(physics, new Vector3(5, 0, 0), 1);
        disabled.Enabled = false;
        var target = AddBody(physics, new Vector3(15, 0, 0), 1);

        Assert.True(physics.Raycast(new Ray(Vector3.Zero, Vector3.UnitX, 100), shooter, out var hit));

        Assert.Same(target, hit.Body);
    }

    [Fact]
    public void Raycast_UsesHitRadius()
    {
        var physics = new PhysicsWorld(null!);
        var body = AddBody(physics, new Vector3(10, 1.5f, 0), 1);
        var ray = new Ray(Vector3.Zero, Vector3.UnitX, 100);

        Assert.False(physics.Raycast(ray, null!, out _));

        body.HitRadius = 2;

        Assert.True(physics.Raycast(ray, null!, out _));
        Assert.Equal(1f, body.Radius);
    }

    [Fact]
    public void Raycast_MissesBeyondLength()
    {
        var physics = new PhysicsWorld(null!);
        AddBody(physics, new Vector3(10, 0, 0), 1);

        Assert.False(physics.Raycast(new Ray(Vector3.Zero, Vector3.UnitX, 8), null!, out _));
    }

    [Fact]
    public void OverlapsSolidBody_IgnoresSelfAndNonSolid()
    {
        var physics = new PhysicsWorld(null!);
        var self = AddBody(physics, Vector3.Zero, 1);
        var ghost = AddBody(physics, new Vector3(1, 0, 0), 1);
        ghost.IsSolid = false;

        Assert.False(physics.OverlapsSolidBody(Vector3.Zero, 1, self, out _));

        var wall = AddBody(physics, new Vector3(1.5f, 0, 0), 1);

        Assert.True(physics.OverlapsSolidBody(Vector3.Zero, 1, self, out var other));
        Assert.Same(wall, other);
        Assert.False(physics.OverlapsSolidBody(new Vector3(-5, 0, 0), 1, self, out _));
    }

    [Fact]
    public void Push_RaisesSpeedAlongNormalOnlyWhenSlower()
    {
        var body = new SphereBody(null!, 1) { Velocity = new Vector3(0, 0, 3) };

        body.Push(Vector3.UnitX, 2);
        Assert.Equal(new Vector3(2, 0, 3), body.Velocity);

        body.Push(Vector3.UnitX, 1);
        Assert.Equal(new Vector3(2, 0, 3), body.Velocity);

        body.IsKinematic = true;
        body.Push(Vector3.UnitY, 5);
        Assert.Equal(new Vector3(2, 0, 3), body.Velocity);
    }
}
