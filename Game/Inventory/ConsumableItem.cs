

namespace Spacebox.Game
{
    public class ConsumableItem : Item
    {
        public byte HealAmount = 5;

        public ConsumableItem(byte stackSize, string name, float modelDepth) : base(stackSize, name, modelDepth)
        {
        }
    }
}
