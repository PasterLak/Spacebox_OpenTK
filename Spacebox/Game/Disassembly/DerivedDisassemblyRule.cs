namespace Spacebox.Game.Disassembly;

public sealed class DerivedDisassemblyRule : IDisassemblyRule
{
    private readonly DisassemblySettings _settings;
    private readonly IReadOnlyList<IDisassemblySourceProvider> _providers;
    private readonly List<DisassemblySource> _sources = new List<DisassemblySource>();

    public DerivedDisassemblyRule(DisassemblySettings settings, IReadOnlyList<IDisassemblySourceProvider> providers)
    {
        _settings = settings;
        _providers = providers;
    }

    public bool TryResolve(Item item, out DisassemblyRecipe? recipe)
    {
        _sources.Clear();
        for (int i = 0; i < _providers.Count; i++)
            _providers[i].Collect(item, _sources);

        _settings.TryGetEntry(item, out var entry);
        recipe = DisassemblyCalculator.Cheapest(
            item,
            _sources,
            entry?.ReturnPercent ?? _settings.DefaultReturnPercent,
            entry?.InputQuantity,
            entry?.DurationTicks,
            _settings.DefaultDurationTicks);

        return true;
    }
}
