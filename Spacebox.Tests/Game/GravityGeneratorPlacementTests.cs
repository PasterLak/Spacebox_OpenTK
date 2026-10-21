using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Tests;

public class GravityGeneratorPlacementTests
{
    private static GravityGeneratorBlock Create(GravityGeneratorJSON settings)
    {
        var data = new GravityGeneratorBlockData(new BlockData("gravity_generator", "gravity"), settings);
        return new GravityGeneratorBlock(data);
    }

    [Fact]
    public void LoadedGeneratorWithoutSavedStateKeepsGravityOn()
    {
        var block = Create(null);

        Assert.True(block.IsGravityEnabled);
        Assert.True(block.IsDefaultConfiguration);
    }

    [Fact]
    public void PlacedGeneratorStartsWithGravityOff()
    {
        var block = Create(null);

        block.OnPlaced();

        Assert.False(block.IsGravityEnabled);
        Assert.False(block.IsDefaultConfiguration);
        Assert.Equal(0, block.ConsumptionRate);
    }

    [Fact]
    public void EnabledOnPlaceKeepsGravityOn()
    {
        var block = Create(new GravityGeneratorJSON { EnabledOnPlace = true });

        block.OnPlaced();

        Assert.True(block.IsGravityEnabled);
    }

    [Fact]
    public void SoundNamesHaveDefaults()
    {
        var data = new GravityGeneratorBlockData(new BlockData("gravity_generator", "gravity"), null);

        Assert.Equal("gravityGeneratorOn", data.EnableSound);
        Assert.Equal("gravityGeneratorOff", data.DisableSound);
    }
}
