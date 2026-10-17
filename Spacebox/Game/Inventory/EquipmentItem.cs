namespace Spacebox.Game;

public class EquipmentItem : Item
{
    public EquipmentSlotType Slot;
    public int HealthBonus;
    public int PowerBonus;

    public EquipmentItem(string name, float modelDepth) : base(1, name, modelDepth)
    {
    }
}
