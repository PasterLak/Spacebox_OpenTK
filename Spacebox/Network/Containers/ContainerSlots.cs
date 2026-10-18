using Spacebox.Game;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Containers;

public static class ContainerSlots
{
    public static SlotState[] Read(IReadOnlyList<Storage> storages)
    {
        var slots = new List<SlotState>();
        for (int s = 0; s < storages.Count; s++)
        {
            foreach (var slot in storages[s].GetAllSlots())
                slots.Add(slot.HasItem ? new SlotState { ItemId = slot.Item.Id, Count = slot.Count } : SlotState.Empty);
        }
        return slots.ToArray();
    }

    public static void Write(IReadOnlyList<Storage> storages, SlotState[] states)
    {
        int index = 0;
        for (int s = 0; s < storages.Count; s++)
        {
            foreach (var slot in storages[s].GetAllSlots())
            {
                if (index >= states.Length) return;

                var state = states[index++];
                if (state.IsEmpty || !GameAssets.TryGetItemById(state.ItemId, out var item))
                {
                    if (slot.HasItem) slot.Clear();
                    continue;
                }

                if (slot.Item != item || slot.Count != state.Count) slot.SetData(item, state.Count);
            }
        }
    }

    public static List<ItemSlot> GetSlots(IReadOnlyList<Storage> storages)
    {
        var slots = new List<ItemSlot>();
        for (int s = 0; s < storages.Count; s++)
            slots.AddRange(storages[s].GetAllSlots());
        return slots;
    }

    public static Dictionary<short, int> Totals(SlotState[] slots)
    {
        var totals = new Dictionary<short, int>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty) continue;
            totals.TryGetValue(slots[i].ItemId, out int total);
            totals[slots[i].ItemId] = total + slots[i].Count;
        }
        return totals;
    }

    public static List<ItemAmount> Deltas(SlotState[] from, SlotState[] to)
    {
        var before = Totals(from);
        var after = Totals(to);
        var deltas = new List<ItemAmount>();

        foreach (var pair in after)
        {
            before.TryGetValue(pair.Key, out int old);
            if (pair.Value != old) deltas.Add(new ItemAmount { ItemId = pair.Key, Amount = pair.Value - old });
        }

        foreach (var pair in before)
        {
            if (!after.ContainsKey(pair.Key)) deltas.Add(new ItemAmount { ItemId = pair.Key, Amount = -pair.Value });
        }

        return deltas;
    }

    public static bool SequenceEqual(SlotState[] a, SlotState[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (!a[i].Equals(b[i])) return false;
        }
        return true;
    }

    public static int RemoveUpTo(List<ItemSlot> slots, Item item, int amount)
    {
        int removed = 0;
        for (int i = slots.Count - 1; i >= 0 && removed < amount; i--)
        {
            var slot = slots[i];
            if (!slot.HasItem || slot.Item.Id != item.Id) continue;

            int take = Math.Min(slot.Count, amount - removed);
            removed += take;

            if (take >= slot.Count) slot.Clear();
            else slot.Count = (byte)(slot.Count - take);
        }
        return removed;
    }

    public static int AddUpTo(List<ItemSlot> slots, Item item, int amount)
    {
        int added = 0;

        for (int i = 0; i < slots.Count && added < amount; i++)
        {
            var slot = slots[i];
            if (!slot.HasItem || slot.Item.Id != item.Id || slot.Count >= item.StackSize) continue;

            int put = Math.Min(item.StackSize - slot.Count, amount - added);
            slot.Count = (byte)(slot.Count + put);
            added += put;
        }

        for (int i = 0; i < slots.Count && added < amount; i++)
        {
            var slot = slots[i];
            if (slot.HasItem || !slot.CanAccept(item)) continue;

            int put = Math.Min((int)item.StackSize, amount - added);
            slot.SetData(item, (byte)put);
            added += put;
        }

        return added;
    }
}
