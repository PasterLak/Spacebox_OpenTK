

namespace Spacebox.Game
{
    public class DrillItem : Item
    {
        public byte Power;

        public DrillItem(byte stackSize, string name, byte x, byte y, float modelDepth) : base(stackSize, name, x, y, modelDepth)
        {
        }
    }
}
