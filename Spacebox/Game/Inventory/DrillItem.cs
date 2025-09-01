

using Engine;

namespace Spacebox.Game
{
    public class DrillItem : Item
    {
        public byte Power = 1;
        public byte PowerUsage = 1;
        public Color3Byte DrillColor;
        public DrillItem(byte stackSize, string name,  float modelDepth) : base(stackSize, name,  modelDepth)
        {
        }
    }
}
