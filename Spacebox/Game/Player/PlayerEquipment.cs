namespace Spacebox.Game.Player;

public class PlayerEquipment
{
    private static readonly EquipmentSlotType[] _slotTypes = Enum.GetValues<EquipmentSlotType>();

    public Storage Storage { get; }
    public event Action OnChanged;

    public int HealthBonus { get; private set; }
    public int PowerBonus { get; private set; }

    public PlayerEquipment()
    {
        Storage = new Storage((byte)_slotTypes.Length, 1) { Name = "Equipment" };

        for (int i = 0; i < _slotTypes.Length; i++)
        {
            var type = _slotTypes[i];
            var slot = Storage.GetSlot(i, 0);
            slot.Name = GetDisplayName(type);
            slot.Filter = item => item is EquipmentItem equipment && equipment.Slot == type;
        }

        Storage.OnDataWasChanged += _ => Recalculate();
    }

    public ItemSlot GetSlot(EquipmentSlotType type)
    {
        return Storage.GetSlot((int)type, 0);
    }

    private void Recalculate()
    {
        int health = 0;
        int power = 0;

        for (int i = 0; i < _slotTypes.Length; i++)
        {
            var slot = Storage.GetSlot(i, 0);
            if (slot.HasItem && slot.Item is EquipmentItem equipment)
            {
                health += equipment.HealthBonus;
                power += equipment.PowerBonus;
            }
        }

        if (health == HealthBonus && power == PowerBonus) return;

        HealthBonus = health;
        PowerBonus = power;
        OnChanged?.Invoke();
    }

    private static string GetDisplayName(EquipmentSlotType type)
    {
        return type switch
        {
            EquipmentSlotType.PowerGenerator => "Power generator",
            _ => type.ToString()
        };
    }
}
