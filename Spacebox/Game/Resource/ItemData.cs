using OpenTK.Mathematics;
using Engine;
namespace Spacebox.Game.Resource
{
    public class ItemData
    {
        public string Info { get; set; } = "";
        public string Description { get; set; } = "";
        public string Name { get; set; } = "NoName";
        public string Type { get; set; } = "item";
        public string Category { get; set; } = "";
        public string Sprite { get; set; } = "";
        public int MaxStack { get; set; } = 1;
        public float ModelDepth { get; set; } = 1.0f;

        public void ValidateMaxStack()
        {
            MaxStack = MathHelper.Abs(MaxStack);
            MaxStack = (byte)MathHelper.Min(MaxStack, byte.MaxValue);
        }
    }

    public class DrillItemData : ItemData
    {
        public byte Power { get; set; } = 0;
        public Color3Byte DrillColor { get; set; } = new Color3Byte(100, 116, 255);
    }

    public class ConsumableItemData : ItemData
    {
        public byte HealAmount { get; set; } = 0;
        public byte PowerAmount { get; set; } = 0;
        public string Sound { get; set; } = "default";
    }

    public class WeaponItemData : ItemData
    {
        public byte Damage { get; set; } = 0;
        public int ReloadTime { get; set; } = 500;
        public byte Spread { get; set; } = 0;
        public byte PowerUsage { get; set; } = 0;
        public byte Pushback { get; set; } = 0;
        public float AnimationSpeed { get; set; } = 1f;
        public string Projectile { get; set; } = "";
        public string ShotSound { get; set; } = "";
    }
}
