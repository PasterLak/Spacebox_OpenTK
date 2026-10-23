using Engine;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class EquipmentUI
{
    private const string BackgroundsFolder = "equipment/";
    public const int Rows = 4;

    public readonly record struct SlotPlacement(EquipmentSlotType Type, int Column, int Row);

    public static readonly SlotPlacement[] ArmorLayout =
    {
        new SlotPlacement(EquipmentSlotType.Helmet, 0, 0),
        new SlotPlacement(EquipmentSlotType.Chest, 0, 1),
        new SlotPlacement(EquipmentSlotType.Legs, 0, 2),
        new SlotPlacement(EquipmentSlotType.Boots, 0, 3),
        new SlotPlacement(EquipmentSlotType.Shoulders, 1, 0),
        new SlotPlacement(EquipmentSlotType.Arms, 1, 1)
    };

    public static readonly EquipmentSlotType[] DeviceSlots =
    {
        EquipmentSlotType.PowerGenerator, EquipmentSlotType.Flashlight, EquipmentSlotType.Scanner
    };

    private static readonly string[] _backgroundNames = CreateBackgroundNames();

    public static PlayerPreview Preview { get; private set; } = new PlayerPreview();

    public static string BackgroundName(EquipmentSlotType type) => _backgroundNames[(int)type];

    private static string[] CreateBackgroundNames()
    {
        var types = Enum.GetValues<EquipmentSlotType>();
        var names = new string[types.Length];
        foreach (var type in types)
        {
            names[(int)type] = BackgroundsFolder + type.ToString().ToLowerInvariant();
        }
        return names;
    }

    public static void RenderPreview()
    {
        Preview.Render();
    }

    public static void Reset()
    {
        Preview.Dispose();
        Preview = new PlayerPreview();
    }

    public static void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }
}
