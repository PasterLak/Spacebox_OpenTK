namespace Spacebox.Game.Disassembly;

public sealed class LegacyRecipeDisassemblyRule : IDisassemblyRule
{
    private readonly IReadOnlyDictionary<string, Dictionary<short, Recipe>> _recipes;
    private readonly string _type;

    public LegacyRecipeDisassemblyRule(IReadOnlyDictionary<string, Dictionary<short, Recipe>> recipes, string type)
    {
        _recipes = recipes;
        _type = type;
    }

    public bool TryResolve(Item item, out DisassemblyRecipe? recipe)
    {
        recipe = null;
        if (!_recipes.TryGetValue(_type, out var byIngredient) || !byIngredient.TryGetValue(item.Id, out var legacy)) return false;
        if (legacy?.Product?.Item == null || legacy.Product.Quantity == 0) return false;

        recipe = new DisassemblyRecipe(item, legacy.Ingredient.Quantity, [new Product(legacy.Product.Item, legacy.Product.Quantity)], legacy.RequiredTicks);
        return true;
    }
}
