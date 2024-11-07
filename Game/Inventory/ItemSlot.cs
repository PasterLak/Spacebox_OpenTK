

using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class ItemSlot
    {
        public Item? Item;
        public byte Count;

        public Storage Storage;
        public Vector2i Position;
        
        public bool IsEmpty => Item == null;


        public ItemSlot(Storage storage, Vector2i position)
        {
            Storage = storage;
            Position = position;
            Item = null;
            Count = 0;
        }

        public ItemSlot(Storage storage, byte x, byte y)
        {
            Storage = storage;
            Position = new Vector2i(x,y);
            Item = null;
            Count = 0;
        }


        public void Clear()
        {
            if (Item != null)
            {
                Item = null;
            }

            Count = 0;
        }

        public bool HasItem => Count > 0;

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

                Count = 0;

                

                if (storage.TryAddItem(Item, count))
                {
                    //Item = item;
                    //Count = count;
                }
                else
                {
                    

                    Debug.Error("Failed to add item to " + storage.Name);
                    Debug.Error("Item name: " + Item.Name);

                    Count = count;
                }
            }
        }
    }
}
