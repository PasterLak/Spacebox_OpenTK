using Spacebox.Game.GUI;

namespace Spacebox.Tests.Game;

public class ItemSearchTests
{
    [Theory]
    [InlineData("Iron Ore", "iron ore")]
    [InlineData("default:iron_ore", "default iron ore")]
    [InlineData("  Steel--Sign ", "steel sign")]
    [InlineData("DrillItem", "drill item")]
    public void Normalize_LowersAndSplitsWords(string text, string expected)
    {
        Assert.Equal(expected, ItemSearch.Normalize(text));
    }

    [Theory]
    [InlineData("IRON ORE")]
    [InlineData("iron")]
    [InlineData("ore")]
    [InlineData("ron")]
    [InlineData("ore iron")]
    [InlineData("ironore")]
    [InlineData("iron_ore")]
    public void Score_FindsCaseInsensitiveAndPartialMatches(string query)
    {
        Assert.NotEqual(ItemSearch.NoMatch, ItemSearch.Score(query, "Iron Ore", "default:iron_ore resources"));
    }

    [Fact]
    public void Score_UsesIdAndCategoryKeywords()
    {
        Assert.NotEqual(ItemSearch.NoMatch, ItemSearch.Score("resources", "Iron Ore", "default:iron_ore resources"));
        Assert.Equal(ItemSearch.NoMatch, ItemSearch.Score("copper", "Iron Ore", "default:iron_ore resources"));
    }

    [Fact]
    public void Score_RanksBetterMatchesFirst()
    {
        int exact = ItemSearch.Score("iron", "Iron", "");
        int prefix = ItemSearch.Score("iron", "Iron Ore", "");
        int word = ItemSearch.Score("iron", "Raw Iron", "");
        int inside = ItemSearch.Score("ron", "Iron", "");
        int keyword = ItemSearch.Score("iron", "Drill", "iron_drill");

        Assert.True(exact < prefix && prefix < word && word < inside && inside < keyword);
    }

    [Fact]
    public void Score_EmptyQueryMatchesEverything()
    {
        Assert.Equal(0, ItemSearch.Score("  ", "Anything", ""));
    }
}
