using Engine;
using System.Text.Json.Serialization;

namespace Spacebox.Game.Resource;

public struct LootResult
{
    public Item Item { get; }
    public byte Quantity { get; }

    public LootResult(Item item, byte quantity)
    {
        Item = item;
        Quantity = quantity;
    }
}

public class LootEntry
{
    [JsonPropertyName("ID")]
    public string ItemIdStr { get; set; } = "";

    [JsonPropertyName("Probability")]
    public int Probability { get; set; } = 100;

    [JsonPropertyName("MinCount")]
    public int MinCount { get; set; } = 1;

    [JsonPropertyName("MaxCount")]
    public int MaxCount { get; set; } = 1;
}

public class LootConfig
{
    public Dictionary<string, List<LootEntry>> LootTables { get; set; } = new Dictionary<string, List<LootEntry>>();

    public void Validate(string modID)
    {
        if (LootTables == null) return;

        foreach (var kv in LootTables)
        {
            var list = kv.Value;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var entry = list[i];

                if (entry.MinCount < 0) entry.MinCount = 0;

                entry.ItemIdStr = GameSetLoader.CombineId(modID, entry.ItemIdStr);

                if (!GameAssets.TryGetItemByFullID(entry.ItemIdStr, out var proto))
                {
                    Debug.Error($"[LootConfig] Item not found: {entry.ItemIdStr} in table {kv.Key}");
                    list.RemoveAt(i);
                }
                else
                {
                    if (entry.MaxCount > proto.StackSize)
                    {
                        entry.MaxCount = proto.StackSize;
                    }

                    if (entry.MinCount > entry.MaxCount)
                    {
                        entry.MinCount = entry.MaxCount;
                    }
                }
            }
        }
    }

    public LootResult[] GenerateLoot(string lootName, int seed, int maxItems = 255)
    {
        if (LootTables == null || !LootTables.TryGetValue(lootName, out var entries))
        {
            return Array.Empty<LootResult>();
        }

        var rolls = LootRoller.Roll(entries, seed, maxItems, id => GameAssets.TryGetItemByFullID(id, out _));
        var results = new LootResult[rolls.Count];
        for (int i = 0; i < rolls.Count; i++)
        {
            GameAssets.TryGetItemByFullID(rolls[i].ItemId, out var proto);
            results[i] = new LootResult(proto, (byte)rolls[i].Count);
        }

        return results;
    }
}