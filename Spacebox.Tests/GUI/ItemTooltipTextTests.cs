using Spacebox.Game;
using Spacebox.Game.GUI;

namespace Spacebox.Tests.GUI;

public class ItemTooltipTextTests
{
    [Fact]
    public void Drill_ListsItsStatsAndDescription()
    {
        var drill = new DrillItem(1, "Drill", 1f) { Description = "Mines soft blocks", Range = 2f, Damage = 5, PowerUsage = 3 };

        string text = ItemTooltipText.Details(drill, false, true);

        Assert.Contains("Mines soft blocks", text);
        Assert.Contains("Range: 2 blocks", text);
        Assert.Contains("Power usage: 3", text);
    }

    [Fact]
    public void Options_AddIdAndStackSize()
    {
        var item = new Item(64, "Stone", 1f) { Id_string = "default:stone" };

        string text = ItemTooltipText.Details(item, true, false, true);

        Assert.Contains("ID: default:stone", text);
        Assert.Contains("Stack: 64", text);
        Assert.Equal("", ItemTooltipText.Details(null, true, true));
    }
}
