using OpenTK.Mathematics;
using Spacebox.Game.Ships.Sharing;

namespace Spacebox.Tests.Ships;

public class ShipPlacementBoundsTests
{
    [Fact]
    public void Contains_UsesRotatedExtents()
    {
        var rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.PiOver2);
        var bounds = ShipPlacementBounds.FromLocal(Vector3.Zero, new Vector3(10f, 2f, 2f), new Vector3(100f, 0f, 0f), rotation);

        Assert.True(bounds.Contains(new Vector3(100f, 0f, 4.5f), 0f));
        Assert.False(bounds.Contains(new Vector3(104.5f, 0f, 0f), 0f));
    }

    [Fact]
    public void Contains_Margin_ExtendsBounds()
    {
        var bounds = new ShipPlacementBounds(Vector3.Zero, new Vector3(1f), Quaternion.Identity);

        Assert.False(bounds.Contains(new Vector3(1.5f, 0f, 0f), 0f));
        Assert.True(bounds.Contains(new Vector3(1.5f, 0f, 0f), 1f));
    }

    [Fact]
    public void Corners_LieOnBoundsAndCoverAllSigns()
    {
        var rotation = Quaternion.FromAxisAngle(Vector3.UnitZ, 0.7f);
        var bounds = new ShipPlacementBounds(new Vector3(5f, -3f, 2f), new Vector3(2f, 1f, 3f), rotation);
        var sum = Vector3.Zero;

        for (int i = 0; i < 8; i++)
        {
            var corner = bounds.Corner(i);
            Assert.True(bounds.Contains(corner, 1e-3f));
            Assert.False(bounds.Contains(corner + (corner - bounds.Center) * 0.01f, 0f));
            sum += corner;
        }

        var center = sum / 8f;
        Assert.Equal(bounds.Center.X, center.X, 3);
        Assert.Equal(bounds.Center.Y, center.Y, 3);
        Assert.Equal(bounds.Center.Z, center.Z, 3);
    }
}
