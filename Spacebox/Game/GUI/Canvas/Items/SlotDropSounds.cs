using Spacebox.Game.GUI.Sounds;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI.Canvas.Items;

public static class SlotDropSounds
{
    public static void Play(ISlotDropTarget target)
    {
        if (target is not ItemSlotControl control) return;

        var slot = control.Slot;
        if (slot != null && slot.Item is EquipmentItem equipment && IsEquipmentSlot(slot))
        {
            EquipmentSounds.Play(equipment);
            return;
        }

        UiSounds.Play(UiSound.SlotPlace);
    }

    private static bool IsEquipmentSlot(ItemSlot slot)
    {
        var equipment = GameUi.Current?.Inventory.Player?.Equipment;
        return equipment != null && slot.Storage != null && ReferenceEquals(slot.Storage, equipment.Storage);
    }
}
