using Engine;

namespace Spacebox.Game.Disassembly;

public sealed class DisassemblySettings
{
    public const int FallbackReturnPercent = 50;
    public const float FallbackDurationSec = 10f;

    private readonly Dictionary<short, DisassemblyEntry> _entries = new Dictionary<short, DisassemblyEntry>();

    public int DefaultReturnPercent { get; }
    public int DefaultDurationTicks { get; }
    public IEnumerable<DisassemblyEntry> Entries => _entries.Values;

    public DisassemblySettings(int defaultReturnPercent, int defaultDurationTicks, IEnumerable<DisassemblyEntry> entries)
    {
        DefaultReturnPercent = Math.Clamp(defaultReturnPercent, 0, 100);
        DefaultDurationTicks = Math.Max(1, defaultDurationTicks);

        foreach (var entry in entries)
        {
            if (entry?.Item != null) _entries[entry.Item.Id] = entry;
        }
    }

    public static DisassemblySettings CreateDefault()
    {
        return new DisassemblySettings(FallbackReturnPercent, Time.SecondsToTicks(FallbackDurationSec), []);
    }

    public bool TryGetEntry(Item item, out DisassemblyEntry entry)
    {
        return _entries.TryGetValue(item.Id, out entry);
    }
}
