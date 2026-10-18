namespace Spacebox.Game.Logistics;

public static class StorageTransfer
{
    public static ItemSlot? FindItem(Storage storage, Predicate<Item> accept)
    {
        for (byte y = 0; y < storage.SizeY; y++)
        for (byte x = 0; x < storage.SizeX; x++)
        {
            var slot = storage.GetSlot(x, y);
            if (slot != null && slot.HasItem && accept(slot.Item)) return slot;
        }
        return null;
    }

    public static ItemSlot? FindSlotFor(Storage storage, Item item, bool filteredSlotsOnly)
    {
        ItemSlot? empty = null;

        for (byte y = 0; y < storage.SizeY; y++)
        for (byte x = 0; x < storage.SizeX; x++)
        {
            var slot = storage.GetSlot(x, y);
            if (slot == null || (filteredSlotsOnly && slot.Filter == null)) continue;

            if (slot.HasItem)
            {
                if (slot.Item.Id == item.Id && slot.Count < item.StackSize) return slot;
            }
            else if (empty == null && slot.CanAccept(item))
            {
                empty = slot;
            }
        }

        return empty;
    }

    public static bool InsertOne(ItemSlot slot, Item item)
    {
        if (slot.HasItem)
        {
            if (slot.Item.Id != item.Id || slot.Count >= item.StackSize) return false;
            slot.Count++;
            return true;
        }

        if (!slot.CanAccept(item)) return false;
        slot.SetData(item, 1);
        return true;
    }
}
