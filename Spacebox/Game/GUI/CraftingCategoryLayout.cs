namespace Spacebox.Game.GUI;

public static class CraftingCategoryLayout
{
    public static CraftingCategory[] Arrange(IEnumerable<CraftingCategory> categories, int rows, out int columns)
    {
        var lines = new List<CraftingCategory>[rows];
        for (int i = 0; i < rows; i++) lines[i] = new List<CraftingCategory>();

        var unplaced = new List<CraftingCategory>();
        foreach (var category in categories)
        {
            if (category.Row >= 1 && category.Row <= rows) lines[category.Row - 1].Add(category);
            else unplaced.Add(category);
        }

        foreach (var category in unplaced)
        {
            var shortest = lines[0];
            for (int i = 1; i < rows; i++)
            {
                if (lines[i].Count < shortest.Count) shortest = lines[i];
            }
            shortest.Add(category);
        }

        columns = 1;
        for (int i = 0; i < rows; i++) columns = Math.Max(columns, lines[i].Count);

        var grid = new CraftingCategory[rows * columns];
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < lines[y].Count; x++) grid[y * columns + x] = lines[y][x];
        }
        return grid;
    }
}
