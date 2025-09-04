using Engine;
using System.Text.Json.Serialization;

namespace Spacebox.Game.Resource
{

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
    public class LootConfig
    {
        [JsonPropertyName("LootTables")]
        public Dictionary<string, List<LootEntry>> LootTables { get; set; }


        public void Validate()
        {

            if (LootTables.ContainsKey(""))
                LootTables.Remove("");

            foreach (var kv in LootTables)
            {
                var list = kv.Value;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var entry = list[i];
                    if (entry.MinCount < 0)
                        entry.MinCount = 0;

                    if (!GameAssets.TryGetItemByFullID(entry.ItemIdStr, out var proto))
                    {

                        list.RemoveAt(i);
                    }
                    else if (entry.MaxCount > proto.StackSize)
                    {

                        entry.MaxCount = proto.StackSize;
                        if (entry.MinCount > entry.MaxCount)
                            entry.MinCount = entry.MaxCount;
                    }
                }
            }
        }


        public LootResult[] GenerateLoot(string lootName, int seed, int maxItems = 255)
        {
            if (!LootTables.TryGetValue(lootName, out var entries))
                return Array.Empty<LootResult>();

            var rng = new Random(seed);
            var results = new List<LootResult>();
            int total = 0;

            foreach (var entry in entries)
            {
                if (total >= maxItems) break;
                if (rng.Next(0, 100) >= entry.Probability) continue;

                int count = entry.MinCount == entry.MaxCount
                    ? entry.MinCount
                    : rng.Next(entry.MinCount, entry.MaxCount + 1);

                if (count <= 0) continue;
                if (!GameAssets.TryGetItemByFullID(entry.ItemIdStr, out var proto)) continue;

                if (total + count > maxItems)
                    count = maxItems - total;

                results.Add(new LootResult(proto, (byte)count));
                total += count;
            }

            return results.ToArray();
        }

        public void Print()
        {
            foreach (var entry in LootTables.Values)
            {
                foreach (var ent in entry)
                {
                    Debug.Log($"{ent.ItemIdStr} prob {ent.Probability} Count {ent.MinCount} - {ent.MaxCount}");
                }
            }
        }
    }

    public class LootEntry
    {
        [JsonPropertyName("ID")]
        public string ItemIdStr { get; set; } = "";

        // 0–100
        [JsonPropertyName("Probability")]
        public int Probability { get; set; } = 100;

        [JsonPropertyName("MinCount")]
        public int MinCount { get; set; } = 1;

        [JsonPropertyName("MaxCount")]
        public int MaxCount { get; set; } = 1;
    }

}
