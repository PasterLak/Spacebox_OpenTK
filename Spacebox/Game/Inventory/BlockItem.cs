using Engine;
namespace Spacebox.Game
{
    public class BlockItem : Item
    {

        public short BlockId;
        public byte Mass = 5;
        public byte Health = 2;

        public BlockItem(short blockId, short id, byte stackSize, string name, byte mass, byte health) : base(id, stackSize, name)
        {
            BlockId = blockId;
            Mass = mass;
            Health = health;
        }
    }
}
