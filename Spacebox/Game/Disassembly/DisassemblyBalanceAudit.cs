namespace Spacebox.Game.Disassembly;

public static class DisassemblyBalanceAudit
{
    public static List<string> FindIssues(DisassemblySettings settings, IReadOnlyList<IDisassemblySourceProvider> providers)
    {
        var issues = new List<string>();
        var sources = new List<DisassemblySource>();

        foreach (var entry in settings.Entries)
        {
            if (entry.Disabled || !entry.HasProducts) continue;

            sources.Clear();
            for (int i = 0; i < providers.Count; i++)
                providers[i].Collect(entry.Item, sources);

            if (sources.Count == 0) continue;

            int inputQuantity = Math.Clamp(entry.InputQuantity ?? 1, 1, byte.MaxValue);
            var fullReturn = DisassemblyCalculator.Cheapest(entry.Item, sources, 100, inputQuantity, null, 1);
            CheckProducts(entry, fullReturn, inputQuantity, issues);
        }

        return issues;
    }

    private static void CheckProducts(DisassemblyEntry entry, DisassemblyRecipe? fullReturn, int inputQuantity, List<string> issues)
    {
        for (int i = 0; i < entry.Products.Count; i++)
        {
            var product = entry.Products[i];
            int allowed = AllowedQuantity(fullReturn, product.Item);

            if (product.Quantity > allowed)
                issues.Add($"{entry.Item.Name} x{inputQuantity} gives {product.Quantity} {product.Item.Name}, but crafting it costs only {allowed}");
        }
    }

    private static int AllowedQuantity(DisassemblyRecipe? recipe, Item item)
    {
        if (recipe == null) return 0;

        for (int i = 0; i < recipe.Products.Count; i++)
        {
            if (recipe.Products[i].Item.Id == item.Id) return recipe.Products[i].Quantity;
        }

        return 0;
    }
}
