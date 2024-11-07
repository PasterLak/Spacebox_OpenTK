
using Spacebox.Common;

namespace Spacebox.Game
{
    public interface IStorage
    {
        public void AddItem();
        public void DeleteItem();
        public bool HasItem();
        public void UpdateItem();
    }

    public class Storage
    {
        private static uint MaxId = 0;

        public uint Id { get; private set; }

        public string Name { get;  set; } = "Default";
        public string Tag { get;  set; } = "Default";

        public byte SizeX { get; private set; } 
        public byte SizeY { get; private set; }

        private ItemSlot[,] Slots;

        public Action<ItemSlot> OnItemAdded;
        public Action<ItemSlot> OnItemDeleted;


        public Storage ConnectedStorage { get; set; }
        public bool MoveItemsToConnectedStorage = false;



        public Storage(byte sizeX, byte sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;

            TakeId();

            Slots = new ItemSlot[SizeX, SizeY];

            FillSlots();
        }


        public void ConnectStorage(Storage storage, bool allowMoveItems = false)
        {
            ConnectedStorage = storage;
            MoveItemsToConnectedStorage = allowMoveItems;
        }

        private void FillSlots()
        {
            for (byte x = 0; x < SizeX; x++)
            {
                for (byte y = 0; y < SizeY; y++)
                {
                    Slots[x,y] = new ItemSlot(this, x,y );

   
                }
            }
        }

        public bool TryAddItem(Item item, byte count)
        {
            if(item == null) return false;
            
            if(TryFindSameItem(item, out ItemSlot slot))
            {
                

                if(slot.Count + count <= item.StackSize)
                {
                    slot.Count += count;
                    return true;
                }
                else
                {
                    byte canPut = (byte)(item.StackSize - slot.Count);

                    slot.Count = item.StackSize;
                    count -= canPut;

                    return TryAddItem(item, count);
                }
            }
            else
            {
                if(TryGetFirstFreeSlot(out ItemSlot slot2))
                {
                    
                    if (count > item.StackSize)
                    {
                        slot2.Item = item;
                        slot2.Count = item.StackSize;

                        count -= item.StackSize;

                        return TryAddItem(item, count);
                    }
                    else if (count <= item.StackSize)
                    {
                       
                        slot2.Item = item;
                        slot2.Count = count;

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
                    if(ConnectedStorage != null && MoveItemsToConnectedStorage)
                    {
                        return ConnectedStorage.TryAddItem(item, count);
                    }

                    return false;
                }
            }


            return false;
        }

        public void DeleteItem()
        {

        }

        public bool HasItem()
        {
            return true;
        }

        public bool HasFreeSlots()
        {
            for(int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (Slots[x,y].Count == 0) return true;
                }
            }

            return false;
        }

        public bool TryGetFirstFreeSlot(out ItemSlot slot)
        {
            slot = null;

            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (Slots[x, y].Count == 0)
                    {
                        slot = Slots[x,y];
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryFindSameItem(Item item, out ItemSlot slot)
        {
            slot = null;

            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    if (Slots[x, y].Count == 0 ) continue;
                    

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


        private void TakeId()
        {
            Id = MaxId;
            MaxId++;
        }

        public void UpdateItem()
        {
            
        }
        public ItemSlot GetSlot(int x, int y)
        {
            if (x >= 0 && x < SizeX && y >= 0 && y < SizeY)
            {
                return Slots[x, y];
            }
            return null;
        }

    }
}
