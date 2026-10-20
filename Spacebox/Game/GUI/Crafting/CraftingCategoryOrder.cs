namespace Spacebox.Game.GUI;

public static class CraftingCategoryOrder
{
    public static void PutBlocksLast(List<CraftingCategory.Data> items)
    {
        var blocks = new List<CraftingCategory.Data>();
        int write = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var data = items[i];
            if (data.item is BlockItem) blocks.Add(data);
            else items[write++] = data;
        }

        for (int i = 0; i < blocks.Count; i++)
            items[write++] = blocks[i];
    }
}
