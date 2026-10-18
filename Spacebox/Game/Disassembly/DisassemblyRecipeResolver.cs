namespace Spacebox.Game.Disassembly;

public sealed class DisassemblyRecipeResolver
{
    private readonly IReadOnlyList<IDisassemblyRule> _rules;
    private readonly Dictionary<short, DisassemblyRecipe?> _cache = new Dictionary<short, DisassemblyRecipe?>();

    public DisassemblyRecipeResolver(IReadOnlyList<IDisassemblyRule> rules)
    {
        _rules = rules;
    }

    public bool TryGet(Item? item, out DisassemblyRecipe recipe)
    {
        recipe = null!;
        if (item == null) return false;

        if (!_cache.TryGetValue(item.Id, out var cached))
        {
            cached = Resolve(item);
            _cache[item.Id] = cached;
        }

        if (cached == null) return false;

        recipe = cached;
        return true;
    }

    private DisassemblyRecipe? Resolve(Item item)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            if (_rules[i].TryResolve(item, out var recipe)) return recipe;
        }

        return null;
    }
}
