namespace Spacebox.Game.Resource.Content.Conversion;

public sealed class ConversionResult
{
    public List<string> Log { get; } = new List<string>();
    public List<string> Differences { get; } = new List<string>();
    public List<string> Errors { get; } = new List<string>();

    public int Blocks { get; set; }
    public int Items { get; set; }
    public int Blueprints { get; set; }
    public int Recipes { get; set; }
    public int MovedFiles { get; set; }

    public bool Success => Errors.Count == 0 && Differences.Count == 0;

    public string Summary => $"{Blocks} blocks, {Items} items, {Blueprints} blueprints, {Recipes} recipes, {MovedFiles} files moved";
}
