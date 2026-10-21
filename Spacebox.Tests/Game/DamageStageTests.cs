using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Tests.Game;

public class DamageStageTests
{
    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(99, 100, 1)]
    [InlineData(67, 100, 1)]
    [InlineData(66, 100, 2)]
    [InlineData(34, 100, 2)]
    [InlineData(33, 100, 3)]
    [InlineData(1, 100, 3)]
    [InlineData(0, 100, 3)]
    [InlineData(5, 0, 0)]
    public void Of_MapsDurabilityToFourStages(int durability, int max, int expected)
    {
        Assert.Equal(expected, DamageStage.Of(durability, max));
    }

    [Fact]
    public void Changed_OnlyWhenCrossingAStage()
    {
        Assert.False(DamageStage.Changed(90, 80, 100));
        Assert.True(DamageStage.Changed(100, 99, 100));
        Assert.True(DamageStage.Changed(70, 60, 100));
        Assert.True(DamageStage.Changed(50, 100, 100));
    }

    [Fact]
    public void Visuals_RemeshAtOnceThenAtMostOncePerInterval()
    {
        var chunk = new Chunk(new Vector3SByte(0, 0, 0), new SpaceEntity(-3, Vector3.Zero, null!), emptyChunk: true);
        int remeshes = 0;
        var visuals = new BlockDamageVisuals(_ => remeshes++, _ => true);

        visuals.MarkIfStageChanged(chunk, 100, 90, 100);
        visuals.Update(0.01f);
        Assert.Equal(1, remeshes);

        visuals.MarkIfStageChanged(chunk, 90, 60, 100);
        visuals.MarkIfStageChanged(chunk, 60, 20, 100);
        visuals.Update(0.1f);
        Assert.Equal(1, remeshes);
        Assert.Equal(1, visuals.PendingCount);

        visuals.Update(BlockDamageVisuals.MinInterval);
        Assert.Equal(2, remeshes);
        Assert.Equal(0, visuals.PendingCount);
    }

    [Fact]
    public void Visuals_IgnoreChangesInsideOneStage()
    {
        var chunk = new Chunk(new Vector3SByte(0, 0, 0), new SpaceEntity(-4, Vector3.Zero, null!), emptyChunk: true);
        int remeshes = 0;
        var visuals = new BlockDamageVisuals(_ => remeshes++, _ => true);

        visuals.MarkIfStageChanged(chunk, 90, 80, 100);
        visuals.Update(1f);

        Assert.Equal(0, remeshes);
    }
}
