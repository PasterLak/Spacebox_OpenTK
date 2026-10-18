using OpenTK.Mathematics;

namespace Spacebox.Game.Logistics;

public sealed class StorageEndpoint : IItemTransferEndpoint
{
    private readonly IReadOnlyList<Storage> _storages;
    private readonly bool _canExtract;
    private readonly bool _filteredSlotsOnly;

    public StorageEndpoint(IReadOnlyList<Storage> storages, bool canExtract, bool filteredSlotsOnly)
    {
        _storages = storages;
        _canExtract = canExtract;
        _filteredSlotsOnly = filteredSlotsOnly;
    }

    public ItemSlot? FindExtractable(Predicate<Item> accept)
    {
        if (!_canExtract) return null;

        for (int i = 0; i < _storages.Count; i++)
        {
            var slot = StorageTransfer.FindItem(_storages[i], accept);
            if (slot != null) return slot;
        }
        return null;
    }

    public void OnExtracted()
    {
    }

    public bool CanInsert(Item item, Vector3i fromSide)
    {
        return FindSlot(item) != null;
    }

    public bool Insert(Item item, Vector3i fromSide)
    {
        var slot = FindSlot(item);
        return slot != null && StorageTransfer.InsertOne(slot, item);
    }

    private ItemSlot? FindSlot(Item item)
    {
        for (int i = 0; i < _storages.Count; i++)
        {
            var slot = StorageTransfer.FindSlotFor(_storages[i], item, _filteredSlotsOnly);
            if (slot != null) return slot;
        }
        return null;
    }
}
