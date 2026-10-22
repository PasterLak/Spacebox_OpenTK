using Spacebox.Game.GUI;

namespace Spacebox.Game.Player;

public static class StartItemPlacement
{
    public const string Auto = "auto";
    public const string Hand = "hand";
    public const string Hotbar = "hotbar";
    public const string Inventory = "inventory";
    public const string Equipment = "equipment";

    public static readonly string[] All = { Auto, Hand, Hotbar, Inventory, Equipment };

    public static bool Give(LocalAstronaut player, Item item, byte count, string putIn, int slotId)
    {
        switch ((putIn ?? Auto).Trim().ToLowerInvariant())
        {
            case Hand:
                int handSlot = Math.Max(0, slotId);
                if (!TryPutInSlot(player.Panel, handSlot, item, count)) return GiveAuto(player, item, count);
                PanelUI.SelectedSlotId = (short)handSlot;
                return true;
            case Hotbar:
                if (slotId >= 0 && TryPutInSlot(player.Panel, slotId, item, count)) return true;
                return player.Panel.TryAddItem(item, count);
            case Inventory:
                return player.Inventory.TryAddItem(item, count) || player.Panel.TryAddItem(item, count);
            case Equipment:
                return player.Equipment.TryEquipIfEmpty(item) || GiveAuto(player, item, count);
            default:
                return GiveAuto(player, item, count);
        }
    }

    private static bool GiveAuto(LocalAstronaut player, Item item, byte count)
    {
        if (player.Equipment.TryEquipIfEmpty(item)) return true;
        return player.Panel.TryAddItem(item, count);
    }

    private static bool TryPutInSlot(Storage storage, int slot, Item item, byte count)
    {
        var target = storage.GetSlot(slot, 0);
        if (target == null || target.HasItem) return false;

        storage.SetSlot(slot, 0, item, count);
        return true;
    }
}
