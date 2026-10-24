using OpenTK.Mathematics;
using Spacebox.Game.Physics;

namespace Spacebox.Tests.Game;

public class BodyPlacementCheckTests
{
    [Fact]
    public void Sphere_InsideOrTouchingCell_Overlaps()
    {
        Assert.True(BodyPlacementCheck.SphereIntersectsBox(Vector3.Zero, Vector3.One, new Vector3(0.5f), 0.1f));
        Assert.True(BodyPlacementCheck.SphereIntersectsBox(Vector3.Zero, Vector3.One, new Vector3(1.3f, 0.5f, 0.5f), 0.4f));
    }

    [Fact]
    public void Sphere_OutsideCell_DoesNotOverlap()
    {
        Assert.False(BodyPlacementCheck.SphereIntersectsBox(Vector3.Zero, Vector3.One, new Vector3(1.5f, 0.5f, 0.5f), 0.4f));
        Assert.False(BodyPlacementCheck.SphereIntersectsBox(Vector3.Zero, Vector3.One, new Vector3(1.4f, 1.4f, 0.5f), 0.5f));
        Assert.False(BodyPlacementCheck.SphereIntersectsBox(Vector3.Zero, Vector3.One, new Vector3(1.45f, 0.5f, 0.5f), 0.4f));
    }

    [Fact]
    public void NoBodies_NeverBlock()
    {
        Assert.False(BodyPlacementCheck.AnyBodyInCell(null, Vector3.Zero, Quaternion.Identity, Vector3.Zero));
        Assert.False(BodyPlacementCheck.AnyBodyInCell(Array.Empty<SphereBody>(), Vector3.Zero, Quaternion.Identity, Vector3.Zero));
    }
}
