namespace Spacebox.Game.Disassembly;

public sealed class DisassemblyEntry
{
    public Item Item { get; init; }
    public bool Disabled { get; init; }
    public int? ReturnPercent { get; init; }
    public int? DurationTicks { get; init; }
    public int? InputQuantity { get; init; }
    public IReadOnlyList<Product> Products { get; init; }

    public bool HasProducts => Products != null && Products.Count > 0;
}
