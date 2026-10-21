using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;

namespace Spacebox.Tests.Game;

public class SupportedBlocksTests
{
    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Forward)]
    [InlineData(Direction.Left)]
    public void Frame_DependsOnlyOnTheBlockBehindIt(Direction direction)
    {
        var shape = PictureFrameGeometry.Shape;
        var cell = new Vector3i(4, 5, 6);
        var offset = shape.GetSupportOffset(direction, Rotation.None);
        var support = cell + new Vector3i(offset.X, offset.Y, offset.Z);

        Assert.NotEqual(cell, support);
        Assert.True(SupportedBlocks.DependsOn(cell, shape, direction, Rotation.None, support));
        Assert.False(SupportedBlocks.DependsOn(cell, shape, direction, Rotation.None, cell - (support - cell)));
    }

    [Fact]
    public void ShapesWithoutSupport_NeverDepend()
    {
        Assert.True(BlockGeometryLibrary.TryGet("slab", out var slab));
        var shape = new BlockShape(slab);
        var cell = new Vector3i(1, 1, 1);

        Assert.False(SupportedBlocks.DependsOn(cell, shape, Direction.Up, Rotation.None, cell + new Vector3i(0, -1, 0)));
        Assert.False(SupportedBlocks.DependsOn(cell, null, Direction.Up, Rotation.None, cell + new Vector3i(0, -1, 0)));
    }
}
