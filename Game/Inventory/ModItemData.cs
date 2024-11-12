using OpenTK.Mathematics;
using System.Text.Json;

namespace Spacebox.Game
{
    public class ModItemData
    {
        public string Info { get; set; } = "";
        public string Name { get; set; } = "NoName";
        public string Type { get; set; } = "item";
        public Vector2Byte TextureCoord { get; set; } = Vector2Byte.Zero;
        public int MaxStack { get; set; } = 1;
        public float ModelDepth { get; set; } = 1.0f;

        public void ValidateMaxStack()
        {
            MaxStack = MathHelper.Abs(MaxStack);
            MaxStack = (byte)MathHelper.Min(MaxStack, byte.MaxValue);
        }
    }

    public class DrillItemData : ModItemData
    {
        public byte Power { get; set; } = 0;
    }

    public class ConsumableItemData : ModItemData
    {
        public byte HealAmount { get; set; } = 0;
    }

    public class WeaponItemData : ModItemData
    {
        public byte Damage { get; set; } = 0;
        public int ReloadTime { get; set; } = 500;
        public byte Spread { get; set; } = 0;
        public byte Pushback { get; set; } = 0;
    }
}
