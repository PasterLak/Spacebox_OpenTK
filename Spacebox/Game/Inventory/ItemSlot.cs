using Engine;
using Engine.UI;

namespace Spacebox.Game
{
    public class ItemSlot
    {
        private static short MaxSlotID = 0;

        public short SlotId;
        public Item? Item;
        public string Name = "";
        public Storage Storage;
        public Vector2Byte Position { get; private set; }

        private byte _count;
        public byte Count
        {
            get => _count;
            set
            {
                _count = value;
                if (Storage != null)
                {
                    Storage.OnDataWasChanged?.Invoke(Storage);
                }
            }
        }


        public ItemSlot(Storage storage, Vector2Byte position)
        {
            Storage = storage;
            Position = position;
            SlotId = MaxSlotID; MaxSlotID++;
            Item = new Item(1, "");
            _count = 0;
        }

        public ItemSlot(Storage storage, byte x, byte y)
        {
            Storage = storage;
            Position = new Vector2Byte(x, y);
            SlotId = MaxSlotID; MaxSlotID++;
            Item = new Item(1, "");
            _count = 0;
        }

        public void SetCount(byte count)
        {
            Count = count;
        }

        public void TakeOne()
        {
            if (!HasItem) return;

            Count--;
        }
        public void AddOne()
        {
            if (!HasItem) return;
            if (HasFreeSpace)
            {
                Count++;
            }
        }

        public void SetData(Item item, byte count)
        {
            Item = item;
            Count = count;
        }

        public void DropOne()
        {
            TakeOne();
        }


        public void Clear()
        {
            Count = 0;
        }

        public void SwapWith(ItemSlot slotToSwapWith)
        {
            var item = Item;
            var count = Count;

            if (Item.Id != slotToSwapWith.Item.Id)
            {
                Item = slotToSwapWith.Item;
                Count = slotToSwapWith.Count;

                slotToSwapWith.Item = item;
                slotToSwapWith.Count = count;
            }
            else
            {
                if (slotToSwapWith.Count == Item.StackSize) return;

                if (Count + slotToSwapWith.Count <= Item.StackSize)
                {
                    Count = 0;
                    slotToSwapWith.Count = (byte)(count + slotToSwapWith.Count);
                }
                else
                {
                    var rest = (Count + slotToSwapWith.Count) - Item.StackSize;

                    slotToSwapWith.Count = Item.StackSize;
                    Count = (byte)rest;
                }
            }
        }

        public bool HasItem => Item != null && Count > 0;
        public bool HasFreeSpace => Item != null && Count < Item.StackSize;
        public void Split()
        {
            if (Count < 2) return;

            byte originalCount = Count;
            byte split1 = (byte)(originalCount / 2);
            byte split2 = (byte)(originalCount - split1);


            Count = Item.StackSize;

            Storage.TryAddItem(Item, split2, out var rest);

            if (rest == 0)
            {
                Count = split1;

            }
            else
            {
                if (Storage.ConnectedStorage != null)
                {
                    Storage.ConnectedStorage.TryAddItem(Item, rest, out rest);
                }

                Count = (byte)(split1 + rest);
            }
            
        }
        public void MoveItemToConnectedStorage()
        {
            if (!HasItem) return;

            Storage target = Storage.ConnectedStorage;

            if (target != null)
            {

                byte count = Count;

                Clear();

                if (!target.TryAddItem(Item, count, out var rest))
                {
                    Count = count;
                }
            }
        }

        public bool TryMoveItemToConnectedStorage(out byte rest)
        {
            rest = 0;
            if (!HasItem) return false;

            Storage target = Storage.ConnectedStorage;

            if (target != null)
            {

                byte count = Count;

                Clear();

                if (target.TryAddItem(Item, count, out rest))
                {
                    return true;
                }
                else
                {
                    Count = rest;
                    return false;
                }
            }

            return false;
        }
    }
}
