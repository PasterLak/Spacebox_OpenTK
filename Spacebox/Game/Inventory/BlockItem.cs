
namespace Spacebox.Game
{
    public class BlockItem : Item
    {

        public short BlockId;
        public byte Mass = 5;
        public byte Durability = 2;

        public BlockItem(short blockId, short id, byte stackSize, string name) : base(id, stackSize, name)
        {
            BlockId = blockId;
        }
    }
}
