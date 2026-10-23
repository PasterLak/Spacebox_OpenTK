namespace Spacebox.Game.GUI;

public sealed class CraftingCategoryGroup
{
    public CraftingCategoryGroup(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public List<CraftingCategory> Categories { get; } = new List<CraftingCategory>();
}

public static class CraftingCategoryGroups
{
    public const string BlocksGroup = "Blocks";
    public const string ItemsGroup = "Items";

    public static List<CraftingCategoryGroup> Build(IEnumerable<CraftingCategory> categories)
    {
        var groups = new List<CraftingCategoryGroup>();
        if (categories == null) return groups;

        foreach (var category in categories)
        {
            if (category == null) continue;
            string name = GroupOf(category);
            var group = groups.Find(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
            if (group == null)
            {
                group = new CraftingCategoryGroup(name);
                groups.Add(group);
            }
            group.Categories.Add(category);
        }

        return groups;
    }

    public static string GroupOf(CraftingCategory category)
    {
        string group = category.Group?.Trim();
        if (!string.IsNullOrEmpty(group)) return group;
        return category.Row == 2 ? ItemsGroup : BlocksGroup;
    }
}
