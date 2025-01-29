namespace Spacebox.Game
{
    public class WeaponItem : Item
    {
        public byte Damage = 0;
        public int ReloadTime = 500;
        public byte Spread  = 0;
        public byte Pushback  = 0;
        public short ProjectileID = 0;
        public float AnimationSpeed = 0;
        public byte PowerUsage = 0;
        public string ShotSound = "";

        public WeaponItem(byte stackSize, string name, float modelDepth) : base(stackSize, name, modelDepth)
        {
        }
    }
}
