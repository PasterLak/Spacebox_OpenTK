using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Tests;

public class BlockDamageTests
{
    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(3, 3, 0)]
    [InlineData(2, 200, 0)]
    public void Apply_SubtractsAndClampsAtZero(byte durability, byte damage, byte expected)
    {
        Assert.Equal(expected, BlockDamage.Apply(durability, damage));
    }

    [Fact]
    public void DamageFromTwoPlayers_AddsUp()
    {
        byte durability = BlockDamage.Apply(BlockDamage.Apply(20, 4), 4);

        Assert.Equal(12, durability);
    }

    [Theory]
    [InlineData(GameMode.Survival, true)]
    [InlineData(GameMode.Creative, false)]
    [InlineData(GameMode.Spectator, false)]
    public void OnlySurvivalPlacementsAreRefunded(GameMode mode, bool refunded)
    {
        Assert.Equal(refunded, PlacementRefund.ConsumesItems(mode));
    }
}
