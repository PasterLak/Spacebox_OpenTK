namespace Spacebox.Game.Disassembly;

public sealed class CraftingDisassemblySourceProvider : IDisassemblySourceProvider
{
    private readonly IReadOnlyDictionary<short, Blueprint> _blueprints;

    public CraftingDisassemblySourceProvider(IReadOnlyDictionary<short, Blueprint> blueprints)
    {
        _blueprints = blueprints;
    }

    public void Collect(Item item, List<DisassemblySource> sources)
    {
        if (!_blueprints.TryGetValue(item.Id, out var blueprint) || blueprint?.Ingredients == null || blueprint.Product == null) return;

        sources.Add(new DisassemblySource(blueprint.Ingredients, blueprint.Product.Quantity, null));
    }
}
