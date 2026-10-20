using Spacebox.Game.Commands;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Network.Admin;

namespace Spacebox.Tests.Game;

public class ChatCommandToolsTests
{
    private static readonly string[] Ids = { "default:iron_ingot", "default:iron_block_heavy", "mymod:iron_ingot", "mymod:laser" };

    [Theory]
    [InlineData("iron_ingot", "default:iron_ingot")]
    [InlineData("IRON_INGOT", "default:iron_ingot")]
    [InlineData("mymod:iron_ingot", "mymod:iron_ingot")]
    [InlineData("laser", "mymod:laser")]
    [InlineData("iron_block_heavy", "default:iron_block_heavy")]
    public void ItemLookup_FindsFullAndShortIds(string query, string expected)
    {
        Assert.Equal(expected, ItemLookup.Resolve(Ids, query, "default"));
    }

    [Fact]
    public void ItemLookup_UnknownOrEmpty_IsNull()
    {
        Assert.Null(ItemLookup.Resolve(Ids, "gold", "default"));
        Assert.Null(ItemLookup.Resolve(Ids, " ", "default"));
        Assert.Equal("iron_ingot", ItemLookup.DisplayId("default:iron_ingot", "default"));
        Assert.Equal("mymod:laser", ItemLookup.DisplayId("mymod:laser", "default"));
    }

    [Fact]
    public void Arguments_SplitAmountAndTarget()
    {
        Assert.True(CommandArguments.TrySplitAmountAndTarget(new[] { "50", "Anna" }, 0, out var amount, out var target));
        Assert.Equal(50, amount);
        Assert.Equal("Anna", target);

        Assert.True(CommandArguments.TrySplitAmountAndTarget(new[] { "Anna" }, 0, out amount, out target));
        Assert.Null(amount);
        Assert.Equal("Anna", target);

        Assert.True(CommandArguments.TrySplitAmountAndTarget(new[] { "glass", "16" }, 1, out amount, out target));
        Assert.Equal(16, amount);
        Assert.Null(target);

        Assert.False(CommandArguments.TrySplitAmountAndTarget(new[] { "1", "2" }, 0, out _, out _));
        Assert.False(CommandArguments.TrySplitAmountAndTarget(new[] { "Anna", "Bob" }, 0, out _, out _));
    }

    [Theory]
    [InlineData(100, PlayerStatsAccess.Full, 100)]
    [InlineData(100, 40, 40)]
    [InlineData(100, 500, 100)]
    [InlineData(100, 0, 0)]
    public void Stats_FullOrClampedValue(int max, int amount, int expected)
    {
        Assert.Equal(expected, PlayerStatsAccess.Target(max, amount));
    }

    [Fact]
    public void Stats_AreDescribed()
    {
        Assert.Equal("health", PlayerStatsAccess.Describe(PlayerStat.Health));
        Assert.Equal("air", PlayerStatsAccess.Describe(PlayerStat.Air));
        Assert.Equal("health, power and air", PlayerStatsAccess.Describe(PlayerStat.All));
    }

    [Fact]
    public void History_BrowsesOlderAndBackToTheDraft()
    {
        var history = new ChatHistory();
        history.Add("/heal");
        history.Add("hello");

        Assert.Equal("hello", history.Older("/gi"));
        Assert.Equal("/heal", history.Older("hello"));
        Assert.Equal("/heal", history.Older("/heal"));
        Assert.Equal("hello", history.Newer("/heal"));
        Assert.Equal("/gi", history.Newer("hello"));
    }

    [Fact]
    public void History_SkipsRepeatsAndEmptyLines()
    {
        var history = new ChatHistory(2);
        history.Add("a");
        history.Add("a");
        history.Add(" ");
        history.Add("b");
        history.Add("c");

        Assert.Equal(2, history.Count);
        Assert.Equal("c", history.Older(""));
        Assert.Equal("b", history.Older("c"));
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/gi", true)]
    [InlineData("/ x", false)]
    [InlineData("hi", false)]
    public void CommandStart_AllowsTheBareSlash(string text, bool expected)
    {
        Assert.Equal(expected, ChatCommands.IsCommandStart(text));
    }

    [Theory]
    [InlineData("give iron", "iron")]
    [InlineData("give ", "")]
    [InlineData("gm", "gm")]
    public void LastWord_IsTheWordBeingTyped(string line, string expected)
    {
        Assert.Equal(expected, ChatCommands.LastWord(line));
    }
}
