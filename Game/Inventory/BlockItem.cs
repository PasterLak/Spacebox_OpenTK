
namespace Spacebox.Game
{
    public class BlockItem : Item
    {

        public short BlockId;

        public BlockItem(short blockId, short id, byte stackSize, string name) : base(id, stackSize, name)
        {
            BlockId = blockId;
        }
    }
}
