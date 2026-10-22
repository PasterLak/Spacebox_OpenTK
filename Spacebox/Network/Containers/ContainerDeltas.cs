using Spacebox.Network.Messages;

namespace Spacebox.Network.Containers;

public static class ContainerDeltas
{
    public static List<ItemAmount> Normalize(List<ItemAmount> deltas)
    {
        var result = new List<ItemAmount>();
        if (deltas == null) return result;

        var totals = new Dictionary<short, long>();
        var order = new List<short>();
        foreach (var delta in deltas)
        {
            if (!totals.ContainsKey(delta.ItemId)) order.Add(delta.ItemId);
            totals.TryGetValue(delta.ItemId, out long sum);
            totals[delta.ItemId] = sum + delta.Amount;
        }

        foreach (var id in order)
        {
            long sum = Math.Clamp(totals[id], int.MinValue + 1, int.MaxValue);
            if (sum != 0) result.Add(new ItemAmount { ItemId = id, Amount = (int)sum });
        }
        return result;
    }

    public static bool MatchesTotals(Dictionary<short, int> before, Dictionary<short, int> after, List<ItemAmount> deltas)
    {
        var expected = new Dictionary<short, int>();
        foreach (var delta in deltas)
        {
            if (expected.ContainsKey(delta.ItemId)) return false;
            expected[delta.ItemId] = delta.Amount;
        }

        foreach (var id in before.Keys.Union(after.Keys).Union(expected.Keys))
        {
            before.TryGetValue(id, out int oldCount);
            after.TryGetValue(id, out int newCount);
            expected.TryGetValue(id, out int delta);
            if (newCount - oldCount != delta) return false;
        }
        return true;
    }
}
