namespace Spacebox.Game.Player;

public static class EquipSoundNames
{
    public const string Armor = "equipArmor";
    public const string Device = "equipDevice";

    public static string Resolve(EquipmentSlotType slot, string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim();
        return IsArmor(slot) ? Armor : Device;
    }

    public static bool IsArmor(EquipmentSlotType slot)
    {
        return slot is EquipmentSlotType.Helmet or EquipmentSlotType.Shoulders or EquipmentSlotType.Chest
            or EquipmentSlotType.Arms or EquipmentSlotType.Legs or EquipmentSlotType.Boots;
    }
}
