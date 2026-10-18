using Spacebox.Game;

namespace Spacebox.Tests;

public class StorageTests
{
    private static readonly Item Ore = new Item(101, 64, "ore") { Id_string = "test:ore" };
    private static readonly Item Ingot = new Item(102, 64, "ingot") { Id_string = "test:ingot" };
    private static readonly Item Rifle = new Item(103, 1, "rifle") { Id_string = "test:rifle" };

    [Fact]
    public void TryAddItem_FillsExistingStackFirst()
    {
        var storage = new Storage(3, 1);
        storage.SetSlot(2, 0, Ore, 60);

        Assert.True(storage.TryAddItem(Ore, 10));

        Assert.Equal(64, storage.GetSlot(2, 0).Count);
        Assert.Equal(6, storage.GetSlot(0, 0).Count);
        Assert.Equal(70, storage.GetTotalCountOf(Ore));
    }

    [Fact]
    public void TryAddItem_ReturnsRestWhenFull()
    {
        var storage = new Storage(1, 1);

        Assert.False(storage.TryAddItem(Ore, 100, out var rest));

        Assert.Equal(64, storage.GetSlot(0, 0).Count);
        Assert.Equal(36, rest);
    }

    [Fact]
    public void TryAddItem_SplitsUnstackableItems()
    {
        var storage = new Storage(2, 1);

        Assert.True(storage.TryAddItem(Rifle, 2));

        Assert.Equal(1, storage.GetSlot(0, 0).Count);
        Assert.Equal(1, storage.GetSlot(1, 0).Count);
    }

    [Fact]
    public void TryAddItem_RespectsFiltersAndManualSlots()
    {
        var storage = new Storage(3, 1);
        storage.GetSlot(0, 0).Filter = item => item.Id == Ingot.Id;
        storage.GetSlot(1, 0).IsManualOnly = true;

        Assert.True(storage.TryAddItem(Ore, 1));

        Assert.False(storage.GetSlot(0, 0).HasItem);
        Assert.False(storage.GetSlot(1, 0).HasItem);
        Assert.True(storage.GetSlot(2, 0).HasItem);
    }

    [Fact]
    public void TryAddItem_OverflowsIntoConnectedStorage()
    {
        var storage = new Storage(1, 1);
        var target = new Storage(1, 1);
        storage.ConnectStorage(target, true);
        storage.SetSlot(0, 0, Ingot, 64);

        Assert.True(storage.TryAddItem(Ore, 5));

        Assert.Equal(5, target.GetTotalCountOf(Ore));
    }

    [Fact]
    public void TryRemoveItem_RemovesAcrossSlotsOrNothing()
    {
        var storage = new Storage(2, 1);
        storage.SetSlot(0, 0, Ore, 3);
        storage.SetSlot(1, 0, Ore, 4);

        Assert.False(storage.TryRemoveItem(Ore, 8));
        Assert.Equal(7, storage.GetTotalCountOf(Ore));

        Assert.True(storage.TryRemoveItem(Ore, 5));
        Assert.Equal(2, storage.GetTotalCountOf(Ore));
        Assert.True(storage.HasItem(Ore, 2));
        Assert.False(storage.HasItem(Ore, 3));
    }

    [Fact]
    public void Clear_EmptiesAndNotifies()
    {
        var storage = new Storage(2, 2);
        storage.SetSlot(1, 1, Ore, 1);
        int notifications = 0;
        storage.OnDataWasChanged += _ => notifications++;

        storage.Clear();

        Assert.False(storage.HasAnyItems());
        Assert.True(notifications > 0);
    }

    [Fact]
    public void CombineStorages_MergesSameItems()
    {
        var a = new Storage(2, 1);
        var b = new Storage(2, 1);
        a.SetSlot(0, 0, Ore, 40);
        b.SetSlot(0, 0, Ore, 40);
        b.SetSlot(1, 0, Ingot, 2);

        var combined = Storage.CombineStorages([a, b], 4, 1);

        Assert.Equal(80, combined.GetTotalCountOf(Ore));
        Assert.Equal(2, combined.GetTotalCountOf(Ingot));
    }

    [Fact]
    public void SortByStackCount_PutsBiggestStacksFirst()
    {
        var storage = new Storage(3, 1);
        storage.SetSlot(0, 0, Ore, 2);
        storage.SetSlot(2, 0, Ingot, 50);

        Storage.SortByStackCount(storage);

        Assert.Equal(Ingot.Id, storage.GetSlot(0, 0).Item.Id);
        Assert.Equal(50, storage.GetSlot(0, 0).Count);
        Assert.Equal(2, storage.GetSlot(1, 0).Count);
        Assert.False(storage.GetSlot(2, 0).HasItem);
    }

    [Fact]
    public void SwapWith_MergesSameItemUpToStackSize()
    {
        var storage = new Storage(2, 1);
        var from = storage.GetSlot(0, 0);
        var to = storage.GetSlot(1, 0);
        from.SetData(Ore, 40);
        to.SetData(Ore, 30);

        from.SwapWith(to);

        Assert.Equal(64, to.Count);
        Assert.Equal(6, from.Count);
    }

    [Fact]
    public void SwapWith_ExchangesDifferentItems()
    {
        var storage = new Storage(2, 1);
        var from = storage.GetSlot(0, 0);
        var to = storage.GetSlot(1, 0);
        from.SetData(Ore, 5);
        to.SetData(Ingot, 7);

        from.SwapWith(to);

        Assert.Equal(Ingot.Id, from.Item.Id);
        Assert.Equal(7, from.Count);
        Assert.Equal(Ore.Id, to.Item.Id);
        Assert.Equal(5, to.Count);
    }

    [Fact]
    public void SwapWith_BlockedByFilter()
    {
        var storage = new Storage(2, 1);
        var from = storage.GetSlot(0, 0);
        var to = storage.GetSlot(1, 0);
        from.SetData(Ore, 5);
        to.Filter = item => item.Id == Ingot.Id;

        from.SwapWith(to);

        Assert.Equal(5, from.Count);
        Assert.False(to.HasItem);
    }

    [Fact]
    public void Split_MovesHalfToFreeSlot()
    {
        var storage = new Storage(2, 1);
        var slot = storage.GetSlot(0, 0);
        slot.SetData(Ore, 9);

        slot.Split();

        Assert.Equal(4, slot.Count);
        Assert.Equal(5, storage.GetSlot(1, 0).Count);
    }
}
