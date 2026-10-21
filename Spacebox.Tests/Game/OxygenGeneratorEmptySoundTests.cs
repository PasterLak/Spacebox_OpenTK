using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Resource;

namespace Spacebox.Tests;

public class OxygenGeneratorEmptySoundTests
{
    private static OxygenGeneratorBlockData Data(OxygenGeneratorJSON settings) => new OxygenGeneratorBlockData(new BlockData("oxygen_generator", "oxygengenerator"), settings, "default");

    [Fact]
    public void EmptySoundHasDefault()
    {
        Assert.Equal("oxygenGeneratorEmpty", Data(null).EmptySound);
    }

    [Fact]
    public void RunningDryWithoutEntityOnlyUpdatesWater()
    {
        var block = new OxygenGeneratorBlock(Data(null));
        block.Restore(3, 0.5f);
        block.Restore(0, 0f);

        Assert.Equal(0, block.Water);
        Assert.False(block.HasWater);
    }
}
