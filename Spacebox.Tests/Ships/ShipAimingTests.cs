using OpenTK.Mathematics;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ShipAimingTests
{
    [Fact]
    public void ClampToCone_InsideCone_Unchanged()
    {
        var direction = new Vector3(0.2f, 1f, 0f).Normalized();

        var result = ShipAiming.ClampToCone(Vector3.UnitY, direction, 60f);

        Assert.True((result - direction).Length < 1e-5f);
    }

    [Fact]
    public void ClampToCone_OutsideCone_StopsAtEdge()
    {
        var result = ShipAiming.ClampToCone(Vector3.UnitY, Vector3.UnitX, 30f);

        Assert.Equal(30f, MathHelper.RadiansToDegrees(ShipAiming.AngleBetween(Vector3.UnitY, result)), 2);
    }

    [Fact]
    public void RotateTowards_Opposite_StillTurns()
    {
        var result = ShipAiming.RotateTowards(Vector3.UnitY, -Vector3.UnitY, 0.5f);

        Assert.Equal(0.5f, ShipAiming.AngleBetween(Vector3.UnitY, result), 3);
    }

    [Fact]
    public void Snapshots_InterpolateBetweenStates()
    {
        var buffer = new ShipSnapshotBuffer();
        buffer.Add(0, Vector3.Zero, Quaternion.Identity, Vector3.Zero);
        buffer.Add(1, new Vector3(10, 0, 0), Quaternion.Identity, Vector3.Zero);

        Assert.True(buffer.TrySample(0.5, out var position, out _));
        Assert.Equal(5f, position.X, 3);
    }

    [Fact]
    public void Snapshots_EmptyBuffer_FailsToSample()
    {
        Assert.False(new ShipSnapshotBuffer().TrySample(1, out _, out _));
    }
}
