using Spacebox.Game;
using Spacebox.Game.Player;
using Xunit;

namespace Spacebox.Tests.Game;

public class ItemUseCueTests
{
    [Fact]
    public void IsReplicated_OnlyForConsumablesAndEquipment()
    {
        Assert.True(ItemUseCue.IsReplicated(new ConsumableItem(1, "food", 1f)));
        Assert.True(ItemUseCue.IsReplicated(new EquipmentItem("helmet", 1f)));
        Assert.False(ItemUseCue.IsReplicated(new Item(1, "ore")));
        Assert.False(ItemUseCue.IsReplicated(null));
    }
}
