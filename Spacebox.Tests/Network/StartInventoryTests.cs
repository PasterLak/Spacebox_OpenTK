using Spacebox.Game;
using Spacebox.Network.Items;

namespace Spacebox.Tests.Network;

public class StartInventoryTests
{
    private static EquipmentItem Equipment(short id, EquipmentSlotType slot) => new EquipmentItem("armor" + id, 0.1f) { Id = id, Slot = slot };

    [Fact]
    public void Totals_EquipOneItemPerSlotAndPutTheRestInTheHotbar()
    {
        var drill = new Item(1, 1, "drill");
        var helmet = Equipment(2, EquipmentSlotType.Helmet);
        var secondHelmet = Equipment(3, EquipmentSlotType.Helmet);

        var totals = StartInventory.Totals(new (Item, int)[] { (drill, 1), (helmet, 4), (secondHelmet, 2), (drill, 0) });

        Assert.Equal(1, totals[1]);
        Assert.Equal(1, totals[2]);
        Assert.Equal(2, totals[3]);
    }

    [Fact]
    public void EquipmentOnly_ListsTheItemsALegacySaveGetsEquipped()
    {
        var drill = new Item(1, 1, "drill");
        var helmet = Equipment(2, EquipmentSlotType.Helmet);

        var totals = StartInventory.EquipmentOnly(new (Item, int)[] { (drill, 1), (helmet, 1) });

        Assert.Single(totals);
        Assert.Equal(1, totals[2]);
    }
}
