namespace Spacebox.Game.Disassembly;

public sealed class DisassemblyRecipe
{
    public Item Input { get; }
    public byte InputQuantity { get; }
    public IReadOnlyList<Product> Products { get; }
    public int DurationTicks { get; }
    public int TotalOutput { get; }

    public DisassemblyRecipe(Item input, byte inputQuantity, IReadOnlyList<Product> products, int durationTicks)
    {
        Input = input;
        InputQuantity = Math.Max((byte)1, inputQuantity);
        Products = products;
        DurationTicks = Math.Max(1, durationTicks);

        for (int i = 0; i < products.Count; i++)
            TotalOutput += products[i].Quantity;
    }

    public bool IsCheaperThan(DisassemblyRecipe other)
    {
        return TotalOutput * other.InputQuantity < other.TotalOutput * InputQuantity;
    }
}
