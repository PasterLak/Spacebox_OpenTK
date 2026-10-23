using System.Text.Json;
using Spacebox.Game;
using Spacebox.Game.GUI;

namespace Spacebox.Tests.GUI;

public class CraftingGroupsAndSettingsTests
{
    [Fact]
    public void Categories_AreGroupedByGroupOrRow()
    {
        var categories = new[]
        {
            new CraftingCategory { Id = "building", Row = 1, Group = "Blocks" },
            new CraftingCategory { Id = "ships", Row = 1, Group = "Ships" },
            new CraftingCategory { Id = "tools", Row = 2 },
            new CraftingCategory { Id = "decor", Row = 1 }
        };

        var groups = CraftingCategoryGroups.Build(categories);

        Assert.Equal(new[] { "Blocks", "Ships", "Items" }, groups.Select(g => g.Name).ToArray());
        Assert.Equal(new[] { "building", "decor" }, groups[0].Categories.Select(c => c.Id).ToArray());
        Assert.Equal("tools", groups[2].Categories.Single().Id);
    }

    [Fact]
    public void OldSettings_SwitchToModernOnce()
    {
        var old = JsonSerializer.Deserialize<InterfaceSettings>("{\"design\":\"classic\"}")!;
        Assert.True(old.Validate(new InterfaceSettings()));
        Assert.Equal(InterfaceSettings.ModernDesign, old.Design);

        old.Design = "classic";
        Assert.False(old.Validate(new InterfaceSettings()));
        Assert.Equal("classic", old.Design);
    }
}
