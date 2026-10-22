using OpenTK.Mathematics;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ShipCollisionMathTests
{
    [Fact]
    public void Hull_SingleCell_HasGridOnEveryFace()
    {
        var hull = new ShipHull();
        hull.AddCell(Vector3i.Zero, _ => false);
        hull.Finish(new Vector3(0.5f));

        Assert.Equal(6 * 9, hull.Samples.Count);
        for (int i = 0; i < hull.Samples.Count; i++)
        {
            var sample = hull.Samples[i];
            var offset = sample.Point - new Vector3(0.5f);
            Assert.Equal(ShipHull.SampleInset, Vector3.Dot(offset, sample.Normal), 4);
            Assert.True(MathF.Abs(offset.X) <= ShipHull.SampleInset + 1e-4f);
            Assert.True(MathF.Abs(offset.Y) <= ShipHull.SampleInset + 1e-4f);
            Assert.True(MathF.Abs(offset.Z) <= ShipHull.SampleInset + 1e-4f);
        }
    }

    [Fact]
    public void Hull_SamplesReachFaceCorners()
    {
        var hull = new ShipHull();
        hull.AddCell(Vector3i.Zero, _ => false);

        bool hasCorner = false;
        for (int i = 0; i < hull.Samples.Count; i++)
            hasCorner |= (hull.Samples[i].Point - new Vector3(0.95f, 0.95f, 0.95f)).Length < 1e-4f;

        Assert.True(hasCorner);
    }

    [Fact]
    public void Hull_SkipsFacesTouchingOwnBlocks()
    {
        var cells = new HashSet<Vector3i> { Vector3i.Zero, Vector3i.UnitX };
        var hull = new ShipHull();
        foreach (var cell in cells) hull.AddCell(cell, cells.Contains);
        hull.Finish(new Vector3(1f, 0.5f, 0.5f));

        Assert.Equal(10 * 9, hull.Samples.Count);
        Assert.True(hull.Radius > 1f);
    }

    [Fact]
    public void Solver_StaticWall_ReflectsWithBounce()
    {
        bool solved = ShipContactSolver.TrySolve(new Vector3(0, 0, -10), Vector3.UnitZ, 1f, 0f, 0.5f, out var impulse);

        Assert.True(solved);
        Assert.Equal(15f, impulse.Z, 4);
    }

    [Fact]
    public void Solver_Separating_DoesNothing()
    {
        Assert.False(ShipContactSolver.TrySolve(new Vector3(0, 0, 3), Vector3.UnitZ, 1f, 1f, 0.2f, out _));
    }

    [Fact]
    public void Solver_EqualBodies_ShareImpulse()
    {
        ShipContactSolver.TrySolve(new Vector3(0, 0, -4), Vector3.UnitZ, 1f, 1f, 0f, out var impulse);

        Assert.Equal(2f, impulse.Z, 4);
    }

    [Fact]
    public void Solver_FrictionIsLimited()
    {
        ShipContactSolver.TrySolve(new Vector3(100, 0, -1), Vector3.UnitZ, 1f, 0f, 0f, out var impulse);

        Assert.Equal(1f, impulse.Z, 4);
        Assert.Equal(-ShipContactSolver.Friction, impulse.X, 4);
    }

    [Fact]
    public void Substeps_GrowWithTravel()
    {
        Assert.Equal(1, ShipContactSolver.GetSubsteps(1f, 0f, 5f, 0.05f, 0.35f, 8));
        Assert.Equal(3, ShipContactSolver.GetSubsteps(20f, 0f, 5f, 0.05f, 0.35f, 8));
        Assert.Equal(8, ShipContactSolver.GetSubsteps(20f, 1.5f, 100f, 0.05f, 0.35f, 8));
    }

    [Fact]
    public void ServerSpeedLimit_CapsButNeverRaisesTheGameSetSpeed()
    {
        var settings = new ShipPhysicsSettings { MaxSpeed = 20f };
        var motion = new ShipMotion { Velocity = new Vector3(0, 0, 100f) };

        motion.IntegrateVelocity(0.01f, Quaternion.Identity, settings, 8f);
        Assert.Equal(8f, motion.Velocity.Length, 3);

        motion.Velocity = new Vector3(0, 0, 100f);
        motion.IntegrateVelocity(0.01f, Quaternion.Identity, settings, 500f);
        Assert.Equal(20f, motion.Velocity.Length, 3);
    }

    [Fact]
    public void InverseMassAlong_IncludesRotation()
    {
        var motion = new ShipMotion();
        motion.SetMassProperties(10f, new Vector3(5f));

        Assert.Equal(0.1f, motion.GetInverseMassAlong(Vector3.Zero, Vector3.UnitZ), 5);
        Assert.Equal(0.1f + 4f / 5f, motion.GetInverseMassAlong(new Vector3(2, 0, 0), Vector3.UnitZ), 5);
    }
}
