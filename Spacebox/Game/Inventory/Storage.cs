using Engine;
using Spacebox.Game.Generation.Blocks;
namespace Spacebox.Game
{

    public class Storage
    {
        private static uint MaxId = 0;

        public readonly uint Id;

        public string Name { get; set; } = "Storage";
        public string Tag { get; set; } = "Default";

        public readonly byte SizeX;
        public readonly byte SizeY;

        public readonly int SlotsCount;

        private ItemSlot[,] _slots;

        public Action<Storage> OnDataWasChanged;


        public Storage ConnectedStorage { get; set; }
        public bool MoveItemsToConnectedStorage = false;


        public Storage(byte sizeX, byte sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SlotsCount = SizeX * SizeY;
            Id = MaxId;
            MaxId++;

            _slots = new ItemSlot[SizeX, SizeY];

            FillSlots();
        }


        public void ConnectStorage(Storage storage, bool allowMoveItems = false)
        {
            ConnectedStorage = storage;
            MoveItemsToConnectedStorage = allowMoveItems;
        }
        public void DisconnectStorage()
        {
            ConnectedStorage = null;
        }

        private void FillSlots()
        {
            for (byte x = 0; x < SizeX; x++)
            {
                for (byte y = 0; y < SizeY; y++)
                {
                    _slots[x, y] = new ItemSlot(this, x, y);
                }
            }
        }

        public void Clear()
        {
            _slots = null;
            _slots = new ItemSlot[SizeX, SizeY];
            FillSlots();
            OnDataWasChanged?.Invoke(this);
        }

        private void OnSlotSuccessFullyUpdated()
        {

            OnDataWasChanged?.Invoke(this);
        }

        public bool TryAddItem(Item item, byte count)
        {
            return TryAddItem(item, count, out var rest);
        }
        public bool TryAddItem(Item item, byte count, out byte rest)
        {
            rest = 0;
            if (item == null) return false;

            if (TryFindUnfilledSlotWithItem(item, out ItemSlot slot))
            {


                if (slot.Count + count <= item.StackSize)
                {
                    slot.Count += count;
                    OnSlotSuccessFullyUpdated();
                    return true;
                }
                else
                {
                    byte canPut = (byte)(item.StackSize - slot.Count);

                    slot.Count = item.StackSize;
                    count -= canPut;

                    return TryAddItem(item, count, out rest);
                }
            }
            else
            {
                if (TryGetFirstFreeSlot(out ItemSlot slot2))
                {

                    if (count > item.StackSize)
                    {
                        slot2.Item = item;
                        slot2.Count = item.StackSize;

                        count -= item.StackSize;

                        return TryAddItem(item, count, out rest);
                    }
                    else if (count <= item.StackSize)
                    {

                        slot2.Item = item;
                        slot2.Count = count;

                        OnSlotSuccessFullyUpdated();
                        return true;
                    }
                    else
                    {
                        slot2.Item = item;
                        slot2.Count = item.StackSize;
                    }
                }
                else
                {
                    if (ConnectedStorage != null && MoveItemsToConnectedStorage)
                    {
                        return ConnectedStorage.TryAddItem(item, count, out rest);
                    }

                    rest = count;

                    return false;
                }
            }
            rest = count;

            return false;
        }

        public void RemoveItem(Item item, byte quantity)
        {
            for (byte x = 0; x < SizeX; x++)
            {
                for (byte y = 0; y < SizeY; y++)
                {
                    var slot = _slots[x, y];

                    if (slot.HasItem && slot.Item.Id == item.Id)
                    {
                        if (slot.Count <= quantity)
                        {

                            quantity -= slot.Count;
                            slot.Count = 0;
                        }
                        else
                        {
                            slot.Count -= quantity;
                            quantity = 0;
                        }

                        if (quantity == 0)
                        {
                            OnSlotSuccessFullyUpdated();
                            return;
                        }
                    }
                }
            }


        }


        public bool TryRemoveItem(Item item, byte quantity)
        {
            if (!HasItem(item, quantity)) return false;

            RemoveItem(item, quantity);
            return true;
        }

        public int GetTotalCountOf(Item item)
        {
            int sum = 0;

            for (byte x = 0; x < SizeX; x++)
            {
                for (byte y = 0; y < SizeY; y++)
                {
                    var slot = _slots[x, y];

                    if (slot.HasItem && slot.Item.Id == item.Id)
                    {
                        sum += slot.Count;
                    }
                }
            }

            return sum;
        }
        public bool HasItem(Item item, byte quantity)
        {

            var q = 0;

            for (byte x = 0; x < SizeX; x++)
            {
                for (byte y = 0; y < SizeY; y++)
                {
                    if (_slots[x, y].Item.Id == item.Id)
                    {
                        q += _slots[x, y].Count;
                    }
                }
            }

            return q >= quantity;
        }

        private bool TryGetFirstFreeSlot(out ItemSlot slot)
        {
            slot = null;

            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (_slots[x, y].Count == 0)
                    {
                        slot = _slots[x, y];
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryFindUnfilledSlotWithItem(Item item, out ItemSlot slot)
        {
            slot = null;

            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (_slots[x, y].Count == 0) continue;


                    if (_slots[x, y].Item.Id == item.Id)
                    {
                        if (_slots[x, y].Count >= item.StackSize) continue;

                        slot = _slots[x, y];
                        return true;
                    }

                }
            }

            return false;
        }

        public IEnumerable<ItemSlot> GetAllSlots()
        {
            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    yield return GetSlot(x, y);
                }
            }
        }


        public static void SortByStackCount(Storage storage)
        {
            var items = new List<(Item item, byte count)>();

            for (int x = 0; x < storage.SizeX; x++)
                for (int y = 0; y < storage.SizeY; y++)
                {
                    var slot = storage.GetSlot(x, y);
                    if (slot.HasItem)
                    {
                        items.Add((slot.Item, slot.Count));
                    }
                }

            var sorted = items
                .OrderByDescending(item => item.count)
                .ThenBy(item => storage.GetItemTypePriority(item.item))
                .ThenBy(item => item.item.Name)
                .ToList();

            int idx = 0;
            for (int x = 0; x < storage.SizeX; x++)
                for (int y = 0; y < storage.SizeY; y++)
                {
                    if (idx < sorted.Count)
                    {
                        var (item, count) = sorted[idx++];
                        storage.GetSlot(x, y).Item = item;
                        storage.GetSlot(x, y).Count = count;
                    }
                    else
                    {
                        storage.GetSlot(x, y).Count = 0;
                    }
                }

            storage.OnDataWasChanged?.Invoke(storage);
        }

        private int GetItemTypePriority(Item item)
        {
            if (item.Is<BlockItem>()) return 0;
            if (item.Is<DrillItem>()) return 1;
            if (item.Is<WeaponItem>()) return 2;
            if (item.Is<ConsumableItem>()) return 3;
            return 4;
        }

        public static void CombineStacks(Storage storage)
        {
            var groups = new Dictionary<short, List<ItemSlot>>();

            foreach (var s in storage.GetAllSlots())
                if (s.HasItem)
                {
                    if (!groups.TryGetValue(s.Item.Id, out var list))
                    {
                        list = new List<ItemSlot>();
                        groups[s.Item.Id] = list;
                    }
                    list.Add(s);
                }

            foreach (var pair in groups)
            {
                var slots = pair.Value;
                int total = slots.Sum(sl => sl.Count);
                byte stackMax = slots[0].Item.StackSize;

                int idx = 0;
                while (total > 0 && idx < slots.Count)
                {
                    byte put = (byte)Math.Min(stackMax, total);
                    slots[idx].Count = put;
                    total -= put;
                    idx++;
                }
                for (int i = idx; i < slots.Count; i++)
                    slots[i].Count = 0;
            }

            storage.OnDataWasChanged?.Invoke(storage);
        }



        public bool HasAnyItems()
        {
            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (_slots[x, y].HasItem) return true;
                }
            }
            return false;
        }
        public ItemSlot? GetSlot(int x, int y)
        {
            if (x >= 0 && x < SizeX && y >= 0 && y < SizeY)
            {
                return _slots[x, y];
            }
            return null;
        }

        public void SetSlot(int x, int y, Item item, byte count)
        {
            _slots[x, y].SetData(item, count);
            OnDataWasChanged?.Invoke(this);
        }


    }
}
