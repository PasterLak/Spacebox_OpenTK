namespace Spacebox.Game.Resource;

public readonly record struct LootRoll(string ItemId, int Count);

public static class LootRoller
{
    public static List<LootRoll> Roll(IReadOnlyList<LootEntry> entries, int seed, int maxItems, Func<string, bool> itemExists)
    {
        var results = new List<LootRoll>();
        if (entries == null) return results;

        var rng = new Random(seed);
        int total = 0;

        foreach (var entry in entries)
        {
            if (total >= maxItems) break;
            if (entry == null) continue;

            if (rng.Next(0, 100) >= entry.Probability) continue;

            int count = entry.MinCount == entry.MaxCount
                ? entry.MinCount
                : rng.Next(entry.MinCount, entry.MaxCount + 1);

            if (count <= 0) continue;
            if (!itemExists(entry.ItemIdStr)) continue;

            if (total + count > maxItems) count = maxItems - total;

            results.Add(new LootRoll(entry.ItemIdStr, count));
            total += count;
        }

        return results;
    }
}
