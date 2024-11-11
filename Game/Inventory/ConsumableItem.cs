

namespace Spacebox.Game
{
    public class ConsumableItem : Item
    {
        public byte HealAmount = 5;

        public ConsumableItem(byte stackSize, string name, byte x, byte y, float modelDepth) : base(stackSize, name, x, y, modelDepth)
        {
        }
    }
}
