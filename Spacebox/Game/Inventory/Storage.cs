﻿
using Spacebox.Game.Generation;
using Engine;
namespace Spacebox.Game
{

    public class Storage
    {
        private static uint MaxId = 0;

        public readonly uint Id;

        public string Name { get; set; } = "Default";
        public string Tag { get; set; } = "Default";

        public readonly byte SizeX;
        public readonly byte SizeY;

        public readonly int SlotsCount;

        private ItemSlot[,] Slots;

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

            Slots = new ItemSlot[SizeX, SizeY];

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
                    Slots[x, y] = new ItemSlot(this, x, y);
                }
            }
        }

        public void Clear()
        {
            Slots = null;
            Slots = new ItemSlot[SizeX, SizeY];
            FillSlots();
            OnDataWasChanged?.Invoke(this);
        }

        private void OnSlotSuccessfullyUpdated()
        {

            OnDataWasChanged?.Invoke(this);
        }

        public bool TryAddBlock(Block block, byte count)
        {

            if (GameBlocks.TryGetItemByBlockID(block.BlockId, out var item))
            {
                return TryAddItem(item, count, out var rest);
            }

            return false;

        }
        public bool TryAddItem(Item item, byte count)
        {
            return TryAddItem(item,count, out var rest);
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
                    OnSlotSuccessfullyUpdated();
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

                        return TryAddItem(item, count,out rest);
                    }
                    else if (count <= item.StackSize)
                    {

                        slot2.Item = item;
                        slot2.Count = count;

                        OnSlotSuccessfullyUpdated();
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
                    //Debug.Error($"Storage {Name}: no empty slots and no connected storage. Items was not added: {count}");
                    return false;
                }
            }
            rest = count;
            //Debug.Error($"{Name}: failed to add items. Items was not added: {count}");
            return false;
        }

        public bool HasItemUnfilledSlot(Item item)
        {
            return TryFindUnfilledSlotWithItem(item, out ItemSlot slot);
        }
        public void DeleteItem(Item item, byte quantity)
        {
            for (byte x = 0; x < SizeX; x++)
            {
                for (byte y = 0; y < SizeY; y++)
                {
                    var slot = Slots[x, y];

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
                            OnSlotSuccessfullyUpdated();
                            return;
                        }
                    }
                }
            }

           
        }


        public bool TryDeleteItem(Item item, byte quantity)
        {
            if (!HasItem(item, quantity)) return false;

           DeleteItem(item, quantity);
            return true;
        }

        public int GetTotalCountOf(Item item)
        {
            int sum = 0;

            for (byte x = 0; x < SizeX; x++)
            {
                for (byte y = 0; y < SizeY; y++)
                {
                    var slot = Slots[x, y];

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
                    if (Slots[x, y].Item.Id == item.Id)
                    {
                        q += Slots[x, y].Count;
                    }
                }
            }

            return q >= quantity;
        }

        private bool HasFreeSlots()
        {
            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (Slots[x, y].Count == 0) return true;
                }
            }

            return false;
        }

        private bool TryGetFirstFreeSlot(out ItemSlot slot)
        {
            slot = null;

            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (Slots[x, y].Count == 0)
                    {
                        slot = Slots[x, y];
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
                    if (Slots[x, y].Count == 0) continue;


                    if (Slots[x, y].Item.Id == item.Id)
                    {
                        if (Slots[x, y].Count >= item.StackSize) continue;

                        slot = Slots[x, y];
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
        public ItemSlot GetSlotByID(int id) // opt todo
        {
            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (Slots[x, y].SlotId == id) return Slots[x, y];
                }
            }
            return Slots[0, 0];
        }
        public ItemSlot GetSlot(int x, int y)
         {
            if (x >= 0 && x < SizeX && y >= 0 && y < SizeY)
            {
                return Slots[x, y];
            }
            return null;
        }

        public void SetSlot(int x, int y, Item item, byte count)
        {
            Slots[x, y].SetData(item, count);
            OnDataWasChanged?.Invoke(this);
        }


        public static Storage operator+ (Storage s1, Storage s2)
        {

            Storage newStorage = new Storage((byte)(s1.SizeX + s2.SizeX), Math.Max(s1.SizeY,s2.SizeY));


            for(int x = 0;x<s1.SizeX;x++)
            {
                for (int y = 0; y < s1.SizeY; y++)
                {
                    var slot = s1.GetSlot(x, y);
                    newStorage.TryAddItem(slot.Item, slot.Count);
                }
            }

            for (int x = 0; x < s2.SizeX; x++)
            {
                for (int y = 0; y < s2.SizeY; y++)
                {
                    var slot = s2.GetSlot(x, y);
                    newStorage.TryAddItem(slot.Item, slot.Count);
                }
            }

            return newStorage;
        }

    }
}
