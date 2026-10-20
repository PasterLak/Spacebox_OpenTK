using System.Text;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Tests;

public class PipeSidesTests
{
    private static PipeBlockData Create(PipeJSON json) => new PipeBlockData(new BlockData("pipe", "pipe"), json);

    [Fact]
    public void Default_InputIsBottomAndOutputsAreTheOtherSides()
    {
        var data = Create(null);

        Assert.Equal(new[] { Direction.Down }, data.InputSides);
        Assert.Equal(new[] { Direction.Up, Direction.Left, Direction.Right, Direction.Forward, Direction.Back }, data.OutputSides);
    }

    [Fact]
    public void Merger_HasThreeInputsAndOneOutput()
    {
        var data = Create(new PipeJSON { InputSides = new[] { "Down", "Left", "Right" }, OutputSides = new[] { "Up" } });

        Assert.Equal(new[] { Direction.Down, Direction.Left, Direction.Right }, data.InputSides);
        Assert.Equal(new[] { Direction.Up }, data.OutputSides);
    }

    [Fact]
    public void Outputs_NeverUseAnInputSide()
    {
        var data = Create(new PipeJSON { InputSides = new[] { "Left" }, OutputSides = new[] { "Left", "Up", "bogus" } });

        Assert.Equal(new[] { Direction.Up }, data.OutputSides);
    }

    [Fact]
    public void Tooltip_ListsSidesAndPower()
    {
        var text = new StringBuilder();
        Create(new PipeJSON { InputSides = new[] { "Down", "Left", "Right" }, OutputSides = new[] { "Up" }, Power = 5 }).AppendTooltip(text);

        Assert.Contains("Inputs: bottom, left, right", text.ToString());
        Assert.Contains("Outputs: top", text.ToString());
        Assert.Contains("Power usage: 5 EU", text.ToString());
    }
}
