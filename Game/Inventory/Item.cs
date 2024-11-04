

namespace Spacebox.Game
{
    public class Item
    {
        public short Id;
        public byte StackSize;
        public string Name;
        public string Discription;

        public Item(short id, byte stackSize, string name)
        {
            Id = id;
            StackSize = stackSize;
            Name = name;
        }
    }
}
