

using Engine;

namespace Spacebox.Game
{
    public class DrillItem : Item
    {
        public byte Tier = 1;
        public byte PowerUsage = 1;
        public float Range = 6;
        public Color3Byte DrillColor;
        public DrillItem(byte stackSize, string name,  float modelDepth) : base(stackSize, name,  modelDepth)
        {
        }
    }
}
