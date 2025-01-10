namespace Spacebox.Game
{
    public class ConsumableItem : Item
    {
        public byte HealAmount = 0;
        public byte PowerAmount = 0;

        public string UseSound;

        public ConsumableItem(byte stackSize, string name, float modelDepth) : base(stackSize, name, modelDepth)
        {
        }
    }
}
