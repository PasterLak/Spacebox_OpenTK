using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Player.Building;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game;

public class PlacementBoundsTests
{
    private readonly List<MultiblockCell> _cells = new List<MultiblockCell>();

    [Fact]
    public void PlainBlock_FillsItsCell()
    {
        PlacementBounds.Get(new BlockData("Block", "block"), Direction.Up, Rotation.None, false, _cells, out var min, out var max);

        Assert.Equal(Vector3.Zero, min);
        Assert.Equal(Vector3.One, max);
    }

    [Fact]
    public void Multiblock_CoversEveryCell()
    {
        var data = new BlockData("Wide", "block") { Size = new Vector3Byte(2, 1, 3) };

        PlacementBounds.Get(data, data.BaseFrontDirection, Rotation.None, false, _cells, out var min, out var max);

        var size = max - min;
        Assert.Equal(6f, size.X * size.Y * size.Z, 3);
        Assert.Equal(3f, MathF.Max(size.X, MathF.Max(size.Y, size.Z)), 3);
    }

    [Fact]
    public void Slab_UsesTheModelBounds()
    {
        Assert.True(BlockGeometryLibrary.TryGet("slab", out var slab));
        var data = new BlockData("Slab", "block") { Shape = new BlockShape(slab) };

        PlacementBounds.Get(data, Direction.Up, Rotation.None, false, _cells, out var min, out var max);

        var size = max - min;
        Assert.True(MathF.Min(size.X, MathF.Min(size.Y, size.Z)) < 0.99f);
    }

    [Fact]
    public void SurfaceCorners_LieOnTheFaceTouchingTheSurface()
    {
        var corners = new Vector3[4];

        PlacementBounds.GetSurfaceCorners(new Vector3(0f, 0f, 0f), new Vector3(2f, 1f, 3f), new Vector3i(0, 1, 0), 0.01f, corners);

        foreach (var corner in corners) Assert.Equal(0.01f, corner.Y, 4);
        Assert.Contains(new Vector3(2f, 0.01f, 3f), corners);
        Assert.Contains(new Vector3(0f, 0.01f, 0f), corners);
    }

    [Fact]
    public void SurfaceCorners_UseTheFarSideForNegativeNormals()
    {
        var corners = new Vector3[4];

        PlacementBounds.GetSurfaceCorners(Vector3.Zero, new Vector3(1f, 1f, 2f), new Vector3i(-1, 0, 0), 0.01f, corners);

        foreach (var corner in corners) Assert.Equal(0.99f, corner.X, 4);
        Assert.Contains(new Vector3(0.99f, 1f, 2f), corners);
    }
}
