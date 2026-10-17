
namespace Spacebox.Game.GUI;

public class CraftingCategory
{
    public string Id { get; set; } = "x";
    public string Name { get; set; } = "x";
    public string Icon { get; set; } = "";
    public IntPtr IconPtr { get; set; } = IntPtr.Zero;
    public int ItemsCountInCategory => Items.Count;
    public List<Data> Items = new List<Data>();

    public class Data
    {
        public Item item;
        public Blueprint blueprint;
    }
}
