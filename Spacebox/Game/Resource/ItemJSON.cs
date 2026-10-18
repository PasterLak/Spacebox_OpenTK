using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.Resource;

public class ItemJSON
{
    public string Info { get; set; } = "";
    public string Description { get; set; } = "";
    public string ID { get; set; } = "no_id";
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

public class DrillItemJSON : ItemJSON
{
    public byte Tier { get; set; } = 0;
    public byte PowerUsage { get; set; } = 0;
    public float Range { get; set; } = 6f;
    public float Damage { get; set; } = 1f;
    public Color3Byte DrillColor { get; set; } = new Color3Byte(100, 116, 255);
}

public class EquipmentJSON : ItemJSON
{
    public byte Tier { get; set; } = 0;
    public string Slot { get; set; } = "";
    public int Health { get; set; } = 0;
    public int Power { get; set; } = 0;
    public FlashlightSettings? Flashlight { get; set; }
    public RadarSettings? Radar { get; set; }
}

public class ConsumableItemJSON : ItemJSON
{
    public byte HealAmount { get; set; } = 0;
    public byte PowerAmount { get; set; } = 0;
    public byte OxygenAmount { get; set; } = 0;
    public float UseCooldown { get; set; } = 0;
    public string Sound { get; set; } = "default";
}

public class MobSpawnerItemJSON : ItemJSON
{
    public string Mob { get; set; } = "";
    public float UseCooldown { get; set; } = 0.5f;
}

public class WeaponItemJSON : ItemJSON
{
    public byte Damage { get; set; } = 0;
    public int ReloadTime { get; set; } = 500;
    public byte Spread { get; set; } = 0;
    public byte PowerUsage { get; set; } = 0;
    public byte AnimationPushback { get; set; } = 0;
    public byte Recoil { get; set; } = 0;
    public float AnimationSpeed { get; set; } = 1f;
    public string Projectile { get; set; } = "";
    public string ShotSound { get; set; } = "";
    public string Ammo { get; set; } = "";
}
