namespace Spacebox.Game.Disassembly;

public sealed class ConfiguredDisassemblyRule : IDisassemblyRule
{
    private readonly DisassemblySettings _settings;

    public ConfiguredDisassemblyRule(DisassemblySettings settings)
    {
        _settings = settings;
    }

    public bool TryResolve(Item item, out DisassemblyRecipe? recipe)
    {
        recipe = null;
        if (!_settings.TryGetEntry(item, out var entry)) return false;
        if (entry.Disabled) return true;
        if (!entry.HasProducts) return false;

        byte inputQuantity = (byte)Math.Clamp(entry.InputQuantity ?? 1, 1, byte.MaxValue);
        recipe = new DisassemblyRecipe(item, inputQuantity, entry.Products, entry.DurationTicks ?? _settings.DefaultDurationTicks);
        return true;
    }
}
