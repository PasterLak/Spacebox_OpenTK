using Spacebox.Network;
using Spacebox.Network.Combat;

namespace Spacebox.Tests.Network;

public class MultiplayerHardeningTests
{
    [Theory]
    [InlineData("Vlad")]
    [InlineData("Влад_2")]
    [InlineData("space cadet")]
    public void PlayerName_AcceptsNormalNames(string name)
    {
        Assert.Null(PlayerNameRules.Validate(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bob?")]
    [InlineData("a/b")]
    [InlineData("a\b")]
    [InlineData("name.")]
    [InlineData("tab\tname")]
    [InlineData("abcdefghijklmnopqrstuvwxyz0123456789")]
    public void PlayerName_RejectsUnsafeNames(string name)
    {
        Assert.NotNull(PlayerNameRules.Validate(name));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(7, false)]
    public void PlayerDamage_IsOnlyAcceptedFromTheServer(int sender, bool accepted)
    {
        Assert.Equal(accepted, CombatNetwork.AcceptsDamageFrom(sender));
    }

    [Fact]
    public void OwnerRotation_RejectsNaNAndZeroQuaternions()
    {
        Assert.True(NetValidation.IsValidRotation(OpenTK.Mathematics.Quaternion.Identity));
        Assert.False(NetValidation.IsValidRotation(new OpenTK.Mathematics.Quaternion(float.NaN, 0f, 0f, 1f)));
        Assert.False(NetValidation.IsValidRotation(new OpenTK.Mathematics.Quaternion(0f, 0f, 0f, 0f)));
        Assert.False(NetValidation.IsValidRotation(new OpenTK.Mathematics.Quaternion(0f, float.PositiveInfinity, 0f, 1f)));
    }

    [Fact]
    public void DrillBudget_LimitsDamagePerSecond()
    {
        var budget = new DrillDamageBudget();
        Assert.Equal(20, budget.Consume(1, 20, 0f));
        Assert.Equal(DrillDamageBudget.MaxDamagePerSecond - 20, budget.Consume(1, 1000, 0.5f));
        Assert.Equal(0, budget.Consume(1, 10, 0.9f));
        Assert.Equal(10, budget.Consume(2, 10, 0.9f));
        Assert.Equal(10, budget.Consume(1, 10, 1.1f));
    }
}
