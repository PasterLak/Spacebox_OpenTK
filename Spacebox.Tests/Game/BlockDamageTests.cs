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

    [Theory]
    [InlineData(10, 3, 20, 13)]
    [InlineData(18, 5, 20, 20)]
    [InlineData(20, 5, 20, 20)]
    [InlineData(30, 5, 20, 30)]
    public void Repair_AddsAndStopsAtMaximum(byte durability, byte amount, byte max, byte expected)
    {
        Assert.Equal(expected, BlockDamage.Repair(durability, amount, max));
    }

    [Theory]
    [InlineData(45, 45, "No repair needed")]
    [InlineData(30, 45, "Durability 66%  (30/45)")]
    [InlineData(0, 10, "Durability 0%  (0/10)")]
    [InlineData(5, 0, "Cannot be repaired")]
    public void RepairHud_DescribesTheBlock(int current, int max, string expected)
    {
        Assert.Equal(expected, Spacebox.Game.GUI.RepairTargetUI.Describe(current, max));
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
