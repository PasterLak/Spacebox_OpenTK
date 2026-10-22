using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using Xunit;

namespace Spacebox.Tests.Game;

public class ProcessingTicksTests
{
    [Theory]
    [InlineData(1, 4f)]
    [InlineData(0, 1f)]
    public void ProcessingNeverTakesZeroTicks(int basicTicks, float efficiency)
    {
        var block = new ResourceProcessingBlock(new BlockData("furnace", "furnace") { Efficiency = efficiency });

        Assert.Equal(1, block.GetTicksWithEfficiency(basicTicks));
    }

    [Fact]
    public void ProcessingTicksScaleWithEfficiency()
    {
        var block = new ResourceProcessingBlock(new BlockData("furnace", "furnace") { Efficiency = 2f });

        Assert.Equal(50, block.GetTicksWithEfficiency(100));
    }
}
