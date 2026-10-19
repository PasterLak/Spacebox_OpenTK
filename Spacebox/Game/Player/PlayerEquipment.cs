namespace Spacebox.Game.Player;

public class PlayerEquipment
{
    private static readonly EquipmentSlotType[] _slotTypes = Enum.GetValues<EquipmentSlotType>();

    public Storage Storage { get; }
    public event Action OnChanged;

    public int HealthBonus { get; private set; }
    public int PowerBonus { get; private set; }
    public float ExplosionProtection { get; private set; }
    public EquipmentItem? FlashlightItem { get; private set; }
    public EquipmentItem? RadarItem { get; private set; }
    public FlashlightSettings? Flashlight => FlashlightItem?.Flashlight;
    public RadarSettings? Radar => RadarItem?.Radar;

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

    public bool TryEquip(ItemSlot source)
    {
        if (source == null || !source.HasItem || source.Item is not EquipmentItem equipment) return false;

        var target = GetSlot(equipment.Slot);
        if (target == source) return false;

        if (!target.HasItem)
        {
            target.SetData(source.Item, source.Count);
            source.Clear();
            return true;
        }

        if (target.Item.Id == source.Item.Id) return false;

        source.SwapWith(target);
        return true;
    }

    public bool TryEquipIfEmpty(Item item)
    {
        if (item is not EquipmentItem equipment) return false;

        var target = GetSlot(equipment.Slot);
        if (target.HasItem) return false;

        target.SetData(item, 1);
        return true;
    }

    private void Recalculate()
    {
        int health = 0;
        int power = 0;
        float explosion = 0f;
        EquipmentItem? flashlight = null;
        EquipmentItem? radar = null;

        for (int i = 0; i < _slotTypes.Length; i++)
        {
            var slot = Storage.GetSlot(i, 0);
            if (slot.HasItem && slot.Item is EquipmentItem equipment)
            {
                health += equipment.HealthBonus;
                power += equipment.PowerBonus;
                explosion += equipment.ExplosionProtection;
                if (equipment.Flashlight != null) flashlight = equipment;
                if (equipment.Radar != null) radar = equipment;
            }
        }

        explosion = ArmorProtection.Combine(explosion);
        if (health == HealthBonus && power == PowerBonus && explosion == ExplosionProtection && flashlight == FlashlightItem && radar == RadarItem) return;

        HealthBonus = health;
        PowerBonus = power;
        ExplosionProtection = explosion;
        FlashlightItem = flashlight;
        RadarItem = radar;
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
