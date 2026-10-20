using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;

namespace Spacebox.Tests;

public class RepairAndCategoryOrderTests
{
    [Fact]
    public void Tally_SumsAllCellsAndFindsTheWeakest()
    {
        var tally = DurabilityTally.Empty;
        tally.Add(0, 100, 100);
        tally.Add(1, 40, 100);
        tally.Add(2, 90, 100);

        Assert.Equal(230, tally.Current);
        Assert.Equal(300, tally.Max);
        Assert.Equal(1, tally.WeakestIndex);
        Assert.True(tally.NeedsRepair);
    }

    [Fact]
    public void Tally_FullStructureNeedsNoRepair()
    {
        var tally = DurabilityTally.Empty;
        tally.Add(0, 50, 50);
        tally.Add(1, 50, 50);

        Assert.False(tally.NeedsRepair);
        Assert.Equal("No repair needed", RepairTargetUI.Describe(tally.Current, tally.Max));
    }

    [Fact]
    public void Category_ListsItemsBeforeBlocksKeepingOrder()
    {
        var items = new List<CraftingCategory.Data>
        {
            new() { item = new BlockItem(1, 10, 1, "bomb", 1, 1) },
            new() { item = new Item(11, 1, "pistol") },
            new() { item = new BlockItem(2, 12, 1, "big bomb", 1, 1) },
            new() { item = new Item(13, 1, "rifle") }
        };

        CraftingCategoryOrder.PutBlocksLast(items);

        Assert.Equal(new[] { "pistol", "rifle", "bomb", "big bomb" }, items.Select(d => d.item.Name).ToArray());
    }
}
