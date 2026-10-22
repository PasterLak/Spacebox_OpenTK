using Spacebox.Network.Messages;

namespace Spacebox.Network.Items;

public sealed class ItemLedger
{
    public const int MaxPending = 512;
    public const float ForceIncludeAfter = 30f;

    private readonly Dictionary<short, int> _base = new Dictionary<short, int>();
    private readonly List<Change> _pending = new List<Change>();
    private int _lastSeq;

    public ItemLedger(IReadOnlyDictionary<short, int> initial)
    {
        Reset(initial);
    }

    public bool TrustNextSnapshot { get; set; }
    public int PendingCount => _pending.Count;
    public int LastSeq => _lastSeq;
    public HashSet<string> ClaimedRewards { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public int Count(short itemId) => _base.TryGetValue(itemId, out int count) ? count : 0;

    public int Available(short itemId)
    {
        int available = Count(itemId);
        for (int i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].ItemId == itemId) available += _pending[i].Delta;
        }
        return Math.Max(0, available);
    }

    public bool Has(IReadOnlyList<ItemAmount> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Amount > 0 && Available(items[i].ItemId) < Needed(items, items[i].ItemId)) return false;
        }
        return true;
    }

    public bool TryLose(short itemId, int amount)
    {
        if (amount <= 0) return true;
        if (Available(itemId) < amount) return false;

        Add(itemId, -amount);
        return true;
    }

    public bool TryLose(IReadOnlyList<ItemAmount> items)
    {
        if (!Has(items)) return false;
        for (int i = 0; i < items.Count; i++) Add(items[i].ItemId, -items[i].Amount);
        return true;
    }

    public void Gain(short itemId, int amount)
    {
        if (amount > 0) Add(itemId, amount);
    }

    public int Push(IReadOnlyList<ItemAmount> deltas, float now)
    {
        int seq = ++_lastSeq;
        for (int i = 0; i < deltas.Count; i++)
        {
            if (deltas[i].Amount != 0) _pending.Add(new Change(seq, deltas[i].ItemId, deltas[i].Amount, now));
        }
        return seq;
    }

    public Dictionary<short, int> Snapshot(IReadOnlyDictionary<short, int> totals, int appliedSeq, float now)
    {
        var excess = new Dictionary<short, int>();
        if (TrustNextSnapshot)
        {
            TrustNextSnapshot = false;
            Reset(totals, keepPending: true);
            _pending.RemoveAll(change => change.Seq <= appliedSeq);
            return excess;
        }

        var expected = new Dictionary<short, int>(_base);
        for (int i = 0; i < _pending.Count; i++)
        {
            var change = _pending[i];
            if (!IsIncluded(change, appliedSeq, now)) continue;
            expected.TryGetValue(change.ItemId, out int count);
            expected[change.ItemId] = count + change.Delta;
        }

        foreach (var pair in totals)
        {
            expected.TryGetValue(pair.Key, out int allowed);
            if (pair.Value > Math.Max(0, allowed)) excess[pair.Key] = pair.Value - Math.Max(0, allowed);
        }

        Reset(totals, keepPending: true);
        _pending.RemoveAll(change => IsIncluded(change, appliedSeq, now));
        return excess;
    }

    public void Reset(IReadOnlyDictionary<short, int> totals, bool keepPending = false)
    {
        _base.Clear();
        if (totals != null)
        {
            foreach (var pair in totals)
            {
                if (pair.Value > 0) _base[pair.Key] = pair.Value;
            }
        }
        if (!keepPending) _pending.Clear();
    }

    private static bool IsIncluded(Change change, int appliedSeq, float now)
    {
        return change.Seq <= appliedSeq || now - change.Time >= ForceIncludeAfter;
    }

    private static int Needed(IReadOnlyList<ItemAmount> items, short itemId)
    {
        int needed = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].ItemId == itemId && items[i].Amount > 0) needed += items[i].Amount;
        }
        return needed;
    }

    private void Add(short itemId, int delta)
    {
        _base.TryGetValue(itemId, out int count);
        _base[itemId] = count + delta;
    }

    private readonly record struct Change(int Seq, short ItemId, int Delta, float Time);
}
