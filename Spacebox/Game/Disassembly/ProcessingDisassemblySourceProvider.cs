namespace Spacebox.Game.Disassembly;

public sealed class ProcessingDisassemblySourceProvider : IDisassemblySourceProvider
{
    private readonly IReadOnlyDictionary<string, Dictionary<short, Recipe>> _recipes;
    private readonly string _excludedType;
    private Dictionary<short, List<DisassemblySource>> _byProduct;

    public ProcessingDisassemblySourceProvider(IReadOnlyDictionary<string, Dictionary<short, Recipe>> recipes, string excludedType)
    {
        _recipes = recipes;
        _excludedType = excludedType;
    }

    public void Collect(Item item, List<DisassemblySource> sources)
    {
        _byProduct ??= BuildIndex();
        if (_byProduct.TryGetValue(item.Id, out var found)) sources.AddRange(found);
    }

    private Dictionary<short, List<DisassemblySource>> BuildIndex()
    {
        var index = new Dictionary<short, List<DisassemblySource>>();

        foreach (var pair in _recipes)
        {
            if (pair.Key == _excludedType) continue;

            foreach (var recipe in pair.Value.Values)
            {
                if (recipe?.Ingredient?.Item == null || recipe.Product?.Item == null) continue;

                short productId = recipe.Product.Item.Id;
                if (!index.TryGetValue(productId, out var list))
                {
                    list = new List<DisassemblySource>();
                    index[productId] = list;
                }

                list.Add(new DisassemblySource([recipe.Ingredient], recipe.Product.Quantity, recipe.RequiredTicks));
            }
        }

        return index;
    }
}
