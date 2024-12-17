

namespace Spacebox.Game
{
    public class DrillItem : Item
    {
        public byte Power = 1;

        public DrillItem(byte stackSize, string name,  float modelDepth) : base(stackSize, name,  modelDepth)
        {
        }
    }
}
