using Spacebox.Game;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Tests.Game;

public sealed class StorageSlotsNbtTests
{
    private static readonly Item Ore = new Item(501, 64, "ore") { Id_string = "test:slots_ore" };

    [Fact]
    public void ItemsOutsideASmallerStorage_GoIntoTheFirstFreeSlot()
    {
        var storage = new Storage(3, 2);
        storage.SetSlot(0, 0, Ore, 1);

        Assert.True(StorageSlotsNbt.PlaceInFirstEmpty(storage, Ore, 7));

        Assert.Equal(1, storage.GetSlot(0, 0).Count);
        Assert.Contains(storage.GetAllSlots(), slot => slot.HasItem && slot.Count == 7);
    }

    [Fact]
    public void AFullStorage_RefusesMoreItems()
    {
        var storage = new Storage(1, 1);
        storage.SetSlot(0, 0, Ore, 1);

        Assert.False(StorageSlotsNbt.PlaceInFirstEmpty(storage, Ore, 2));
    }
}
