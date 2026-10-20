using Spacebox.Game.Player;
using Spacebox.Game.Rendering;

namespace Spacebox.Tests.Game;

public class OxygenWarningTests
{
    [Theory]
    [InlineData(100, 0f)]
    [InlineData(20, 0f)]
    [InlineData(10, 0.5f)]
    [InlineData(0, 1f)]
    public void Level_StartsBelowTwentyPercent(int oxygen, float expected)
    {
        Assert.Equal(expected, OxygenWarning.Level(oxygen, 100), 3);
    }

    [Fact]
    public void Level_WithoutMaxOxygen_IsZero()
    {
        Assert.Equal(0f, OxygenWarning.Level(0, 0));
    }

    [Fact]
    public void Vignette_WithoutWarning_FollowsSetting()
    {
        var state = VignetteController.Compute(0.3f, 0f);

        Assert.Equal(0.3f, state.Strength, 3);
        Assert.Equal(VignetteController.BaseRadius, state.Radius, 3);
        Assert.True(state.Enabled);
        Assert.False(state.AlwaysApply);
    }

    [Fact]
    public void Vignette_SettingOff_StaysOffWithoutWarning()
    {
        var state = VignetteController.Compute(0f, 0f);

        Assert.False(state.Enabled);
    }

    [Theory]
    [InlineData(0.5f, 0.75f)]
    [InlineData(0.8f, 1.2f)]
    [InlineData(0f, 0.75f)]
    public void Vignette_AtZeroOxygen_IsOneAndAHalfTimesStronger(float setting, float expected)
    {
        var state = VignetteController.Compute(setting, 1f);

        Assert.Equal(expected, state.Strength, 3);
        Assert.Equal(VignetteController.WarningRadius, state.Radius, 3);
        Assert.True(state.Enabled);
        Assert.True(state.AlwaysApply);
    }

    [Fact]
    public void Suffocation_DealsDamageEverySecondAtZeroOxygen()
    {
        var suffocation = new SuffocationDamage();

        Assert.Equal(0, suffocation.Update(0.5f, true));
        Assert.Equal(SuffocationDamage.DamagePerTick, suffocation.Update(0.5f, true));
        Assert.Equal(SuffocationDamage.DamagePerTick * 2, suffocation.Update(2f, true));
    }

    [Fact]
    public void Suffocation_ResetsWhenOxygenReturns()
    {
        var suffocation = new SuffocationDamage();
        suffocation.Update(0.9f, true);

        Assert.Equal(0, suffocation.Update(0.1f, false));
        Assert.Equal(0, suffocation.Update(0.5f, true));
    }
}
