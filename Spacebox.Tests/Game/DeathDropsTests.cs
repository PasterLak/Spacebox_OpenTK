using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Rules;

namespace Spacebox.Tests.Game;

public class DeathDropsTests
{
    private static readonly Item Ore = new Item(301, 64, "ore") { Id_string = "test:death_ore" };
    private static readonly Item Helmet = new Item(302, 1, "helmet") { Id_string = "test:death_helmet" };

    [Fact]
    public void TakeAll_EmptiesEveryStorageAndMergesStacks()
    {
        var panel = new Storage(4, 1);
        var inventory = new Storage(4, 2);
        var equipment = new Storage(2, 1);
        panel.SetSlot(0, 0, Ore, 20);
        inventory.SetSlot(3, 1, Ore, 64);
        equipment.SetSlot(1, 0, Helmet, 1);

        var stacks = DeathDrops.TakeAll(panel, inventory, null, equipment);

        Assert.Equal(2, stacks.Count);
        Assert.Contains(new ItemStack(Ore, 84), stacks);
        Assert.Contains(new ItemStack(Helmet, 1), stacks);
        Assert.False(panel.HasAnyItems());
        Assert.False(inventory.HasAnyItems());
        Assert.False(equipment.HasAnyItems());
    }

    [Fact]
    public void TakeAll_ReturnsNothingForEmptyStorages()
    {
        Assert.Empty(DeathDrops.TakeAll(new Storage(2, 2)));
    }

    [Theory]
    [InlineData(true, GameMode.Survival, false)]
    [InlineData(false, GameMode.Survival, true)]
    [InlineData(false, GameMode.Creative, false)]
    [InlineData(false, GameMode.Spectator, false)]
    public void OnlySurvivalPlayersDropWhenInventoryIsNotKept(bool keepInventory, GameMode mode, bool drops)
    {
        Assert.Equal(drops, DeathDrops.ShouldDrop(new ServerRules { KeepInventory = keepInventory }, GameModeRules.ForBase(mode)));
    }
}
