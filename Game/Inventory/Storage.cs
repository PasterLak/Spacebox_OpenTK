

namespace Spacebox.Game.Inventory
{
    public interface IStorage
    {
        public void AddItem();
        public void DeleteItem();
        public bool HasItem();
        public void UpdateItem();
    }

    public class Storage : IStorage
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

        public void AddItem()
        {

        }

        public void DeleteItem()
        {

        }

        public bool HasItem()
        {
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
