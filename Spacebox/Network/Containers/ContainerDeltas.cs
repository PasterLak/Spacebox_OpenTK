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
}
