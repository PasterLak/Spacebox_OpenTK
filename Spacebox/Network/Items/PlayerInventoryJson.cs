using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spacebox.Network.Items;

public static class PlayerInventoryJson
{
    public const string ClaimedRewardsField = "ServerClaimedRewards";

    private static readonly string[] SlotLists = { "InventorySlots", "PanelSlots", "EquipmentSlots" };
    private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions { WriteIndented = true };

    public static bool TryReadTotals(string json, Func<string, short?> resolve, out Dictionary<short, int> totals, out bool hasEquipment)
    {
        totals = new Dictionary<short, int>();
        hasEquipment = false;
        if (!TryParse(json, out var root)) return false;

        hasEquipment = root["EquipmentSlots"] is JsonArray;
        foreach (var list in SlotLists)
        {
            if (root[list] is not JsonArray slots) continue;
            foreach (var slot in slots)
            {
                if (!TryReadSlot(slot, resolve, out short itemId, out int count)) continue;
                totals.TryGetValue(itemId, out int current);
                totals[itemId] = current + count;
            }
        }
        return true;
    }

    public static string RemoveItems(string json, Func<string, short?> resolve, IReadOnlyDictionary<short, int> remove)
    {
        if (remove == null || remove.Count == 0 || !TryParse(json, out var root)) return json;

        var left = new Dictionary<short, int>(remove);
        foreach (var list in SlotLists)
        {
            if (root[list] is not JsonArray slots) continue;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (!TryReadSlot(slots[i], resolve, out short itemId, out int count)) continue;
                if (!left.TryGetValue(itemId, out int wanted) || wanted <= 0) continue;

                int taken = Math.Min(count, wanted);
                left[itemId] = wanted - taken;
                if (taken == count) slots.RemoveAt(i);
                else slots[i]["Count"] = count - taken;
            }
        }
        return root.ToJsonString(WriteOptions);
    }

    public static IReadOnlyList<string> ReadStrings(string json, string field)
    {
        var result = new List<string>();
        if (!TryParse(json, out var root) || root[field] is not JsonArray values) return result;

        foreach (var value in values)
        {
            if (value is JsonValue text && text.TryGetValue(out string item) && !string.IsNullOrWhiteSpace(item)) result.Add(item);
        }
        return result;
    }

    public static string WriteStrings(string json, string field, IEnumerable<string> values)
    {
        if (!TryParse(json, out var root)) return json;

        var array = new JsonArray();
        foreach (var value in values) array.Add(JsonValue.Create(value));
        root[field] = array;
        return root.ToJsonString(WriteOptions);
    }

    private static bool TryReadSlot(JsonNode slot, Func<string, short?> resolve, out short itemId, out int count)
    {
        itemId = 0;
        count = 0;
        if (slot is not JsonObject obj || obj["ItemID"] is not JsonValue idValue || obj["Count"] is not JsonValue countValue) return false;
        if (!idValue.TryGetValue(out string fullId) || !countValue.TryGetValue(out count) || count <= 0) return false;

        var resolved = resolve(fullId);
        if (resolved == null) return false;
        itemId = resolved.Value;
        return true;
    }

    private static bool TryParse(string json, out JsonObject root)
    {
        root = null;
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            root = JsonNode.Parse(json) as JsonObject;
            return root != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
