

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

        public string Name { get; private set; } = "Default";
        public string Tag { get; private set; } = "Default";

        public byte SizeX { get; private set; } 
        public byte SizeY { get; private set; }

        private ItemSlot[,] Slots;

        public Storage(byte sizeX, byte sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;

            TakeId();

            Slots = new ItemSlot[SizeX, SizeY];
        }

        public bool TryAddItem(Item item, short count)
        {
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
            for(int x = 0; x < Slots.GetLength(0); x++)
            {
                for (int y = 0; y < Slots.GetLength(1); y++)
                {

                }
            }

            return true;
        }


        private void TakeId()
        {
            Id = MaxId;
            MaxId++;
        }

        public void UpdateItem()
        {
            
        }
    }
}
