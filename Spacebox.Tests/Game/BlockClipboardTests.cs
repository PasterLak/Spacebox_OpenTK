using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player.Building;

namespace Spacebox.Tests.Game;

public class BlockClipboardTests
{
    [Fact]
    public void FourTurns_ReturnTheOriginalOrientation()
    {
        for (byte d = 0; d < 6; d++)
        {
            for (byte r = 0; r < 4; r++)
            {
                var direction = (Direction)d;
                var rotation = (Rotation)r;
                var original = BlockRotationHelper.CalculateTransformMatrix(Direction.Up, direction, rotation);
                for (int i = 0; i < 4; i++) BlockTurn.Turn(Direction.Up, direction, rotation, out direction, out rotation);
                var turned = BlockRotationHelper.CalculateTransformMatrix(Direction.Up, direction, rotation);

                for (int row = 0; row < 3; row++)
                    for (int column = 0; column < 3; column++)
                        Assert.Equal(original[row, column], turned[row, column], 3);
            }
        }
    }

    [Fact]
    public void Turn_KeepsUpAndMovesSides()
    {
        BlockTurn.Turn(Direction.Up, Direction.Up, Rotation.None, out var up, out _);
        Assert.Equal(Direction.Up, up);

        BlockTurn.Turn(Direction.Up, Direction.Left, Rotation.None, out var side, out _);
        Assert.NotEqual(Direction.Left, side);
        Assert.NotEqual(Direction.Up, side);
        Assert.NotEqual(Direction.Down, side);
    }

    [Fact]
    public void TurnOffset_StaysInsideTheTurnedBox()
    {
        var size = new Vector3i(3, 2, 5);
        var turnedSize = BlockTurn.TurnSize(size);
        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
                for (int z = 0; z < size.Z; z++)
                {
                    var turned = BlockTurn.TurnOffset(new Vector3i(x, y, z), size);
                    Assert.InRange(turned.X, 0, turnedSize.X - 1);
                    Assert.Equal(y, turned.Y);
                    Assert.InRange(turned.Z, 0, turnedSize.Z - 1);
                }
    }

    [Fact]
    public void FourTurns_ReturnTheOriginalCells()
    {
        var cells = new List<ClipboardCell> { new ClipboardCell(new Vector3i(0, 0, 0), 1, Direction.Up, Rotation.None), new ClipboardCell(new Vector3i(2, 1, 4), 1, Direction.Left, Rotation.Right) };
        var clipboard = new BlockClipboard(new Vector3i(3, 2, 5), new List<ClipboardCell>(cells));

        for (int i = 0; i < 4; i++) clipboard.Turn(_ => Direction.Up);

        Assert.Equal(new Vector3i(3, 2, 5), clipboard.Size);
        Assert.Equal(cells[0].Offset, clipboard.Cells[0].Offset);
        Assert.Equal(cells[1].Offset, clipboard.Cells[1].Offset);
    }

    [Fact]
    public void PasteMin_SitsOnTheClickedFace()
    {
        var size = new Vector3i(4, 3, 2);
        Assert.Equal(new Vector3i(8, 5, 9), BlockClipboard.PasteMin(new Vector3i(10, 5, 10), Vector3i.UnitY, size));
        Assert.Equal(new Vector3i(8, 3, 9), BlockClipboard.PasteMin(new Vector3i(10, 5, 10), -Vector3i.UnitY, size));
        Assert.Equal(new Vector3i(10, 4, 9), BlockClipboard.PasteMin(new Vector3i(10, 5, 10), Vector3i.UnitX, size));
    }
}
