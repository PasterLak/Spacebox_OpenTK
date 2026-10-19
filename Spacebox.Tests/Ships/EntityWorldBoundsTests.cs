using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class EntityWorldBoundsTests
{
    [Fact]
    public void Compute_WithoutRotation_KeepsBox()
    {
        EntityWorldBounds.Compute(new Vector3(0, 0, 0), new Vector3(2, 4, 6), Vector3.Zero, Quaternion.Identity, out var min, out var max);

        Assert.Equal(new Vector3(0, 0, 0), min);
        Assert.Equal(new Vector3(2, 4, 6), max);
    }

    [Fact]
    public void Compute_RotatedAroundPivot_SwapsAxes()
    {
        var rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.PiOver2);

        EntityWorldBounds.Compute(new Vector3(0, 0, 0), new Vector3(4, 1, 1), Vector3.Zero, rotation, out var min, out var max);

        Assert.Equal(1f, max.X - min.X, 3);
        Assert.Equal(4f, max.Z - min.Z, 3);
    }

    [Fact]
    public void Overlaps_DetectsTouchingAndSeparateBoxes()
    {
        Assert.True(EntityWorldBounds.Overlaps(Vector3.Zero, Vector3.One, new Vector3(1, 0, 0), new Vector3(2, 1, 1)));
        Assert.False(EntityWorldBounds.Overlaps(Vector3.Zero, Vector3.One, new Vector3(1.1f, 0, 0), new Vector3(2, 1, 1)));
    }

    [Fact]
    public void MassProperties_MergeEqualsSingleAccumulation()
    {
        var all = new ShipMassProperties();
        all.Add(new Vector3(1, 0, 0), 2f);
        all.Add(new Vector3(-3, 2, 5), 7f);

        var a = new ShipMassProperties();
        a.Add(new Vector3(1, 0, 0), 2f);
        var b = new ShipMassProperties();
        b.Add(new Vector3(-3, 2, 5), 7f);
        a.Merge(b);

        Assert.Equal(all.Mass, a.Mass);
        Assert.Equal(all.CenterOfMass, a.CenterOfMass);
        Assert.Equal(all.GetInertia(), a.GetInertia());
    }
}
