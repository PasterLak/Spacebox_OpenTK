using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class ItemSlot
    {
        private static short MaxSlotID = 0;
        public short SlotId;
        public Item? Item;
        public byte Count;

        public Storage Storage;
        public Vector2i Position;
        
  
        public ItemSlot(Storage storage, Vector2i position)
        {
            Storage = storage;
            Position = position;
            SlotId = MaxSlotID; MaxSlotID++;
            Item = new Item(1,"");
            Count = 0;
        }

        public ItemSlot(Storage storage, byte x, byte y)
        {
            Storage = storage;
            Position = new Vector2i(x,y);
            SlotId = MaxSlotID; MaxSlotID++;
            Item = new Item(1, "");
            Count = 0;
        }

        public void TakeOne()
        {
            if (!HasItem) return;

            Count--;

            Storage.OnDataWasChanged?.Invoke(Storage);
        }

        public void DropOne()
        {
            TakeOne();
        }


        public void Clear()
        {
            
            Count = 0;
            Storage.OnDataWasChanged?.Invoke(Storage);
        }

        public void SwapWith(ItemSlot slotToSwapWith)
        {
            var item = Item;
            var count = Count;

            if(Item.Id != slotToSwapWith.Item.Id)
            {
                Item = slotToSwapWith.Item;
                Count = slotToSwapWith.Count;

                slotToSwapWith.Item = item;
                slotToSwapWith.Count = count;
            }
            else
            {

                if (slotToSwapWith.Count == Item.StackSize) return;

                if(Count + slotToSwapWith.Count <= Item.StackSize)
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
            

            Storage.OnDataWasChanged?.Invoke(Storage);
            slotToSwapWith.Storage.OnDataWasChanged?.Invoke(slotToSwapWith.Storage);
        }

        public bool HasItem => Item != null && Count > 0;

        public void Split()
        {
            if (Count < 2) return;

            byte count = Count;

            Count = Item.StackSize;

            byte split1 = (byte)(count / 2);
            byte split2 = (byte)(split1 + (count % 2));

            if (Storage.TryAddItem(Item, split2))
            {
                //Item = item;
                Storage.OnDataWasChanged?.Invoke(Storage);
               
                Count = split1;
            }
            else
            {


                Debug.Error("Failed to split items to " + Storage.Name);
                Debug.Error("Item name: " + Item.Name);

                Count = count;
            }
        }
        public void MoveItemToConnectedStorage()
        {
            if (!HasItem) return;

            Storage storage = Storage.ConnectedStorage;

            if (storage != null)
            {

                byte count = Count;

                Clear();

                if (storage.TryAddItem(Item, count))
                {
                    Storage.OnDataWasChanged?.Invoke(Storage);
                    storage.OnDataWasChanged?.Invoke(storage);
                }
                else
                {
    
                    Debug.Error("Failed to add item to " + storage.Name + " by moving from " + Storage.Name);
                    Debug.Error("Item name: " + Item.Name);

                    Count = count;
                }
            }
        }
    }
}
