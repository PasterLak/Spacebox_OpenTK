namespace Spacebox.Game.Resource.Content;

public sealed class LayerContent
{
    public List<ContentEntry> Blocks { get; } = new List<ContentEntry>();
    public List<ContentEntry> Items { get; } = new List<ContentEntry>();
    public List<ContentEntry> Categories { get; } = new List<ContentEntry>();
    public List<ContentEntry> Blueprints { get; } = new List<ContentEntry>();
    public List<ContentEntry> Recipes { get; } = new List<ContentEntry>();

    public bool HasBlocks { get; set; }
    public bool HasItems { get; set; }
    public bool HasCrafting { get; set; }
    public bool HasRecipes { get; set; }
}
