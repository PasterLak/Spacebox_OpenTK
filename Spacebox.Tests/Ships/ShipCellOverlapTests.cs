using OpenTK.Mathematics;
using Spacebox.Game.Ships.Sharing;

namespace Spacebox.Tests.Ships;

public class ShipCellOverlapTests
{
    private static ShipPlacementBounds LShape(Vector3 center)
    {
        var cells = new ShipCellSet(new[] { new Vector3i(0, 0, 0), new Vector3i(1, 0, 0), new Vector3i(2, 0, 0), new Vector3i(0, 1, 0), new Vector3i(0, 2, 0) });
        return ShipPlacementBounds.FromLocal(Vector3.Zero, new Vector3(3f, 3f, 1f), center, Quaternion.Identity, cells);
    }

    [Fact]
    public void BlockInsideAShipCell_Overlaps()
    {
        var bounds = LShape(new Vector3(100f, 100f, 100f));

        Assert.True(bounds.Overlaps(new Vector3(98.5f, 98.5f, 100f), 0.4f));
    }

    [Fact]
    public void BlockInTheEmptyCornerOfTheBox_DoesNotOverlap()
    {
        var bounds = LShape(new Vector3(100f, 100f, 100f));

        Assert.True(bounds.Contains(new Vector3(101f, 101f, 100f), 0f));
        Assert.False(bounds.Overlaps(new Vector3(101f, 101f, 100f), 0.4f));
    }

    [Fact]
    public void RotatedShip_UsesItsOwnCells()
    {
        var cells = new ShipCellSet(new[] { new Vector3i(0, 0, 0), new Vector3i(1, 0, 0), new Vector3i(2, 0, 0), new Vector3i(0, 1, 0), new Vector3i(0, 2, 0) });
        var bounds = ShipPlacementBounds.FromLocal(Vector3.Zero, new Vector3(3f, 3f, 1f), Vector3.Zero, Quaternion.FromAxisAngle(Vector3.UnitZ, MathHelper.PiOver2), cells);

        Assert.True(bounds.Overlaps(new Vector3(1f, -1f, 0f), 0.2f));
        Assert.False(bounds.Overlaps(new Vector3(-1f, 1f, 0f), 0.2f));
    }

    [Fact]
    public void WithoutCells_TheBoxIsUsed()
    {
        var bounds = ShipPlacementBounds.FromLocal(Vector3.Zero, new Vector3(3f, 3f, 1f), Vector3.Zero, Quaternion.Identity);

        Assert.True(bounds.Overlaps(new Vector3(1f, 1f, 0f), 0f));
    }
}
