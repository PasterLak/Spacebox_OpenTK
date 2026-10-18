using Engine.Networking;
using Spacebox.Game;
using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public class ContainerSlotsTests
{
    private static readonly Item Ore = new Item(901, 64, "ore") { Id_string = "nettest:ore" };
    private static readonly Item Cell = new Item(902, 16, "cell") { Id_string = "nettest:cell" };

    private static SlotState State(Item item, byte count) => new SlotState { ItemId = item.Id, Count = count };

    [Fact]
    public void Read_ListsAllSlotsOfAllStoragesInOrder()
    {
        var input = new Storage(2, 1);
        var output = new Storage(1, 1);
        input.SetSlot(1, 0, Ore, 5);
        output.SetSlot(0, 0, Cell, 2);

        var states = ContainerSlots.Read([input, output]);

        Assert.Equal(3, states.Length);
        Assert.True(states[0].IsEmpty);
        Assert.Equal(State(Ore, 5), states[1]);
        Assert.Equal(State(Cell, 2), states[2]);
    }

    [Fact]
    public void Write_AppliesStatesAndClearsEmptyOrUnknown()
    {
        GameAssets.Items[Ore.Id] = Ore;
        GameAssets.Items[Cell.Id] = Cell;
        try
        {
            var storage = new Storage(3, 1);
            storage.SetSlot(0, 0, Ore, 9);
            storage.SetSlot(2, 0, Ore, 9);

            ContainerSlots.Write([storage], [SlotState.Empty, State(Cell, 3), new SlotState { ItemId = 999, Count = 1 }]);

            Assert.False(storage.GetSlot(0, 0).HasItem);
            Assert.Equal(Cell.Id, storage.GetSlot(1, 0).Item.Id);
            Assert.Equal(3, storage.GetSlot(1, 0).Count);
            Assert.False(storage.GetSlot(2, 0).HasItem);
        }
        finally
        {
            GameAssets.Items.Remove(Ore.Id);
            GameAssets.Items.Remove(Cell.Id);
        }
    }

    [Fact]
    public void Deltas_ReportAddedRemovedAndChangedTotals()
    {
        SlotState[] before = [State(Ore, 10), State(Ore, 5), State(Cell, 1)];
        SlotState[] after = [State(Ore, 12), SlotState.Empty, SlotState.Empty, State(Cell, 0)];

        var deltas = ContainerSlots.Deltas(before, after);

        Assert.Contains(deltas, d => d.ItemId == Ore.Id && d.Amount == -3);
        Assert.Contains(deltas, d => d.ItemId == Cell.Id && d.Amount == -1);
        Assert.Equal(2, deltas.Count);
    }

    [Fact]
    public void Deltas_MovingItemsBetweenSlots_IsNoChange()
    {
        SlotState[] before = [State(Ore, 10), SlotState.Empty];
        SlotState[] after = [SlotState.Empty, State(Ore, 10)];

        Assert.Empty(ContainerSlots.Deltas(before, after));
        Assert.False(ContainerSlots.SequenceEqual(before, after));
    }

    [Fact]
    public void SlotState_EmptyStatesAreEqual()
    {
        Assert.Equal(SlotState.Empty, new SlotState { ItemId = Ore.Id, Count = 0 });
        Assert.True(ContainerSlots.SequenceEqual([SlotState.Empty], [new SlotState { ItemId = 5, Count = 0 }]));
        Assert.False(ContainerSlots.SequenceEqual([SlotState.Empty], [SlotState.Empty, SlotState.Empty]));
        Assert.False(ContainerSlots.SequenceEqual(null!, [SlotState.Empty]));
    }

    [Fact]
    public void RemoveUpTo_TakesFromLastSlotsAndCapsAtAvailable()
    {
        var storage = new Storage(3, 1);
        storage.SetSlot(0, 0, Ore, 4);
        storage.SetSlot(2, 0, Ore, 3);
        var slots = ContainerSlots.GetSlots([storage]);

        Assert.Equal(5, ContainerSlots.RemoveUpTo(slots, Ore, 5));
        Assert.Equal(2, storage.GetSlot(0, 0).Count);
        Assert.False(storage.GetSlot(2, 0).HasItem);

        Assert.Equal(2, ContainerSlots.RemoveUpTo(slots, Ore, 50));
        Assert.False(storage.HasAnyItems());
    }

    [Fact]
    public void AddUpTo_FillsStacksThenEmptyAcceptingSlots()
    {
        var storage = new Storage(3, 1);
        storage.SetSlot(1, 0, Cell, 14);
        storage.GetSlot(0, 0).Filter = item => item.Id == Ore.Id;
        var slots = ContainerSlots.GetSlots([storage]);

        int added = ContainerSlots.AddUpTo(slots, Cell, 40);

        Assert.Equal(18, added);
        Assert.Equal(16, storage.GetSlot(1, 0).Count);
        Assert.Equal(16, storage.GetSlot(2, 0).Count);
        Assert.False(storage.GetSlot(0, 0).HasItem);
    }

    [Fact]
    public void SlotStateArray_RoundTripsOverNetwork()
    {
        SlotState[] states = [State(Ore, 64), SlotState.Empty, new SlotState { ItemId = Cell.Id, Count = 0 }];
        var writer = new NetWriter();
        SlotState.WriteArray(writer, states);

        var read = SlotState.ReadArray(new NetReader(writer.ToArray()));

        Assert.True(ContainerSlots.SequenceEqual(states, read));
        Assert.Equal(-1, read[2].ItemId);
    }

    [Fact]
    public void ItemAmountList_RoundTripsOverNetwork()
    {
        var amounts = new List<ItemAmount> { new ItemAmount { ItemId = Ore.Id, Amount = -300 }, new ItemAmount { ItemId = Cell.Id, Amount = 7 } };
        var writer = new NetWriter();
        ItemAmount.WriteList(writer, amounts);

        var read = ItemAmount.ReadList(new NetReader(writer.ToArray()));

        Assert.Equal(2, read.Count);
        Assert.Equal((Ore.Id, -300), (read[0].ItemId, read[0].Amount));
        Assert.Equal((Cell.Id, 7), (read[1].ItemId, read[1].Amount));
    }
}
