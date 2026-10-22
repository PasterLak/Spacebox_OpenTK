using Spacebox.Game;
using Spacebox.Game.Resource;

namespace Spacebox.Network.Items;

public static class StartInventory
{
    public static Dictionary<short, int> Totals(IEnumerable<(Item Item, int Count)> items)
    {
        var totals = new Dictionary<short, int>();
        var usedSlots = new HashSet<EquipmentSlotType>();
        foreach (var (item, count) in items)
        {
            if (item == null || count < 1) continue;
            int amount = item is EquipmentItem equipment && usedSlots.Add(equipment.Slot) ? 1 : count;
            totals.TryGetValue(item.Id, out int current);
            totals[item.Id] = current + amount;
        }
        return totals;
    }

    public static Dictionary<short, int> EquipmentOnly(IEnumerable<(Item Item, int Count)> items)
    {
        var totals = new Dictionary<short, int>();
        var usedSlots = new HashSet<EquipmentSlotType>();
        foreach (var (item, _) in items)
        {
            if (item is not EquipmentItem equipment || !usedSlots.Add(equipment.Slot)) continue;
            totals.TryGetValue(item.Id, out int current);
            totals[item.Id] = current + 1;
        }
        return totals;
    }

    public static List<(Item Item, int Count)> FromGameSet()
    {
        var result = new List<(Item, int)>();
        var config = GameSetLoader.ModInfo;
        if (config?.ItemsOnStart == null) return result;

        foreach (var entry in config.ItemsOnStart)
        {
            if (string.IsNullOrEmpty(entry.ID) || entry.Count < 1) continue;
            if (GameAssets.TryGetItemByFullID(GameSetLoader.CombineId(config.ModId, entry.ID), out var item)) result.Add((item, entry.Count));
        }
        return result;
    }
}
