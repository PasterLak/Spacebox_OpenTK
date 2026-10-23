using Spacebox.Game.Generation.Blocks.Components;
using Xunit;

namespace Spacebox.Tests.Game;

public class ProcessingTicksTests
{
    [Theory]
    [InlineData(1, 4f)]
    [InlineData(0, 1f)]
    public void ProcessingNeverTakesZeroTicks(int basicTicks, float efficiency)
    {
        var processing = new ProcessingComponent(new ProcessingComponentDefinition("furnace", 0, efficiency));

        Assert.Equal(1, processing.GetTicksWithEfficiency(basicTicks));
    }

    [Fact]
    public void ProcessingTicksScaleWithEfficiency()
    {
        var processing = new ProcessingComponent(new ProcessingComponentDefinition("furnace", 0, 2f));

        Assert.Equal(50, processing.GetTicksWithEfficiency(100));
    }
}
