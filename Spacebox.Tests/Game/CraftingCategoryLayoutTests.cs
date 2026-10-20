using Spacebox.Game.GUI;

namespace Spacebox.Tests;

public class CraftingCategoryLayoutTests
{
    private static CraftingCategory Category(string id, int row = 0) => new CraftingCategory { Id = id, Name = id, Row = row };

    [Fact]
    public void Rows_FollowTheRowField()
    {
        var grid = CraftingCategoryLayout.Arrange(new[] { Category("blocks", 1), Category("drills", 2), Category("lights", 1), Category("tools", 2), Category("items", 2) }, 2, out int columns);

        Assert.Equal(3, columns);
        Assert.Equal(new[] { "blocks", "lights", null, "drills", "tools", "items" }, grid.Select(c => c?.Id).ToArray());
    }

    [Fact]
    public void CategoriesWithoutRow_FillTheShorterRow()
    {
        var grid = CraftingCategoryLayout.Arrange(new[] { Category("a"), Category("b"), Category("c") }, 2, out int columns);

        Assert.Equal(2, columns);
        Assert.Equal(new[] { "a", "c", "b", null }, grid.Select(c => c?.Id).ToArray());
    }
}
