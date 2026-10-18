using Spacebox.Game.GUI;

namespace Spacebox.Game.Disassembly;

public static class DisassemblyCalculator
{
    public static DisassemblyRecipe? FromSource(Item item, DisassemblySource source, int returnPercent, int? inputQuantity, int durationTicks)
    {
        int productQuantity = Math.Max(1, (int)source.ProductQuantity);
        int input = Math.Clamp(inputQuantity ?? productQuantity, 1, byte.MaxValue);
        int percent = Math.Clamp(returnPercent, 0, 100);
        var products = new List<Product>();

        for (int i = 0; i < source.Ingredients.Count; i++)
        {
            var ingredient = source.Ingredients[i];
            if (!IsRecoverable(item, ingredient)) continue;

            int quantity = ingredient.Quantity * percent * input / (100 * productQuantity);
            if (quantity > 0) Add(products, ingredient.Item, quantity);
        }

        return products.Count == 0 ? null : new DisassemblyRecipe(item, (byte)input, products, durationTicks);
    }

    public static DisassemblyRecipe? Cheapest(Item item, IReadOnlyList<DisassemblySource> sources, int returnPercent, int? inputQuantity, int? durationTicks, int defaultDurationTicks)
    {
        DisassemblyRecipe? best = null;

        for (int i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var recipe = FromSource(item, source, returnPercent, inputQuantity, durationTicks ?? source.DurationTicks ?? defaultDurationTicks);
            if (recipe == null) return null;
            if (best == null || recipe.IsCheaperThan(best)) best = recipe;
        }

        return best;
    }

    private static bool IsRecoverable(Item item, Ingredient ingredient)
    {
        if (ingredient?.Item == null || ingredient.Quantity == 0) return false;
        if (ingredient.Item.Id == item.Id) return false;
        return ingredient.Item.Id_string == null || !CraftingLogic.IsVirtualResource(ingredient.Item.Id_string);
    }

    private static void Add(List<Product> products, Item item, int quantity)
    {
        for (int i = 0; i < products.Count; i++)
        {
            if (products[i].Item.Id != item.Id) continue;

            products[i].Quantity = (byte)Math.Min(byte.MaxValue, products[i].Quantity + quantity);
            return;
        }

        products.Add(new Product(item, (byte)Math.Min(byte.MaxValue, quantity)));
    }
}
