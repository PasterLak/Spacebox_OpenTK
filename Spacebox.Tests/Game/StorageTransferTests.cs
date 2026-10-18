using Spacebox.Game;
using Spacebox.Game.Logistics;

namespace Spacebox.Tests;

public class StorageTransferTests
{
    private static readonly Item Ore = new Item(111, 64, "ore") { Id_string = "test:ore" };
    private static readonly Item Water = new Item(112, 64, "water") { Id_string = "test:water" };

    [Fact]
    public void FindSlotFor_PrefersExistingStack()
    {
        var storage = new Storage(3, 1);
        storage.SetSlot(2, 0, Ore, 10);

        Assert.Same(storage.GetSlot(2, 0), StorageTransfer.FindSlotFor(storage, Ore, false));
    }

    [Fact]
    public void FindSlotFor_FilteredOnly_SkipsUnfilteredSlots()
    {
        var storage = new Storage(2, 1);
        storage.GetSlot(1, 0).Filter = item => item.Id == Water.Id;

        Assert.Same(storage.GetSlot(1, 0), StorageTransfer.FindSlotFor(storage, Water, true));
        Assert.Null(StorageTransfer.FindSlotFor(storage, Ore, true));
    }

    [Fact]
    public void FindSlotFor_FullStorage_ReturnsNull()
    {
        var storage = new Storage(1, 1);
        storage.SetSlot(0, 0, Ore, 64);

        Assert.Null(StorageTransfer.FindSlotFor(storage, Ore, false));
    }

    [Fact]
    public void InsertOne_RespectsStackAndFilter()
    {
        var storage = new Storage(2, 1);
        var full = storage.GetSlot(0, 0);
        full.SetData(Ore, 64);
        var filtered = storage.GetSlot(1, 0);
        filtered.Filter = item => item.Id == Water.Id;

        Assert.False(StorageTransfer.InsertOne(full, Ore));
        Assert.False(StorageTransfer.InsertOne(filtered, Ore));
        Assert.True(StorageTransfer.InsertOne(filtered, Water));
        Assert.Equal(1, filtered.Count);
    }

    [Fact]
    public void FindItem_ReturnsFirstAccepted()
    {
        var storage = new Storage(2, 1);
        storage.SetSlot(0, 0, Ore, 1);
        storage.SetSlot(1, 0, Water, 1);

        Assert.Same(storage.GetSlot(1, 0), StorageTransfer.FindItem(storage, item => item.Id == Water.Id));
        Assert.Null(StorageTransfer.FindItem(storage, _ => false));
    }
}
