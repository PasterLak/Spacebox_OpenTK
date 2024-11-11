
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spacebox.Game
{
    public class ModItemData
    {
        public string Info { get; set; } = "";
        public string Name { get; set; } = "NoName";
        public string Type { get; set; } = "Item";
        public Vector2Byte TextureCoord { get; set; } = Vector2Byte.Zero;
        public int MaxStack { get; set; } = 1;
        public float ModelDepth { get; set; } = 1.0f;
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


        public static bool TryParse(JsonElement itemElement)
        {
            var weaponData = itemElement.Deserialize<WeaponItemData>();
            if (weaponData == null) return false;

            var weaponItem = new WeaponeItem(
                (byte)weaponData.MaxStack,
                weaponData.Name,
                weaponData.TextureCoord.X,
                weaponData.TextureCoord.Y,
                weaponData.ModelDepth);

            weaponItem.Damage = weaponData.Damage;
            GameBlocks.RegisterItem(weaponItem);
            return true;
        }
    }



    
}
