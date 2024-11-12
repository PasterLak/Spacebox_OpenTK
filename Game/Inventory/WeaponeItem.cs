namespace Spacebox.Game
{
    public class WeaponeItem : Item
    {
        public byte Damage = 0;
        public int ReloadTime = 500;
        public byte Spread  = 0;
        public byte Pushback  = 0;

        public WeaponeItem(byte stackSize, string name, byte x, byte y, float modelDepth) : base(stackSize, name, x, y, modelDepth)
        {
        }
    }
}
