namespace Spacebox.Game.Player;

public static class EquipSoundNames
{
    public static string Resolve(EquipmentSlotType slot, string configured)
    {
        return string.IsNullOrWhiteSpace(configured) ? "" : configured.Trim();
    }

    public static bool IsArmor(EquipmentSlotType slot)
    {
        return slot is EquipmentSlotType.Helmet or EquipmentSlotType.Shoulders or EquipmentSlotType.Chest
            or EquipmentSlotType.Arms or EquipmentSlotType.Legs or EquipmentSlotType.Boots;
    }
}
