using Spacebox.Game.Mobs;

namespace Spacebox.Tests.GUI;

public class MobVisionTests
{
    [Theory]
    [InlineData(14f, 3f, true, false, 14f)]
    [InlineData(14f, 3f, false, false, 3f)]
    [InlineData(14f, 3f, false, true, 14f)]
    [InlineData(14f, 0f, false, false, 14f)]
    public void MobNoticeRange_DependsOnTheFlashlight(float sight, float dark, bool light, bool provoked, float expected)
    {
        Assert.Equal(expected, MobVision.NoticeRange(sight, dark, light, provoked));
    }

    [Fact]
    public void WatchRoll_NeverHappensWithZeroChance()
    {
        var random = new Random(1);
        for (int i = 0; i < 100; i++) Assert.False(MobVision.RollWatch(0f, random));
        Assert.True(MobVision.RollWatch(1f, random));
    }
}
