using System.Numerics;
using Engine.Graphics.Atlas;
using Engine;
using ImGuiNET;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class EquipmentUI
{
    private const int Columns = 3;
    private const string Title = "Equipment";
    private const string BackgroundsFolder = "equipment/";
    private const float SlotGapRatio = 0.1f;

    private static readonly EquipmentSlotType[] _armorSlots =
    {
        EquipmentSlotType.Helmet, EquipmentSlotType.Shoulders, EquipmentSlotType.Chest,
        EquipmentSlotType.Arms, EquipmentSlotType.Legs, EquipmentSlotType.Boots
    };

    private static readonly EquipmentSlotType[] _upgradeSlots =
    {
        EquipmentSlotType.PowerGenerator, EquipmentSlotType.Flashlight, EquipmentSlotType.Scanner
    };

    private static readonly Action<ItemSlot> _onSlotClicked = OnSlotClicked;
    private static readonly string[] _backgroundNames = CreateBackgroundNames();

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

    public static void OnGUI(PlayerEquipment equipment)
    {
        if (equipment == null || !UIManager.IsOpen("inventory")) return;

        var displaySize = ImGui.GetIO().DisplaySize;
        float slotSize = InventoryUIHelper.SlotSize;
        float padding = slotSize * 0.1f;
        float slotStep = slotSize * (1f + SlotGapRatio);
        float titleHeight = padding * 4;
        float upgradesGap = padding * 3;

        float contentWidth = Columns * slotStep;
        float armorHeight = (_armorSlots.Length / Columns) * slotStep;
        var windowSize = new Vector2(contentWidth + padding * 2, titleHeight + armorHeight + upgradesGap + slotStep + padding * 2);
        var windowPos = new Vector2(InventoryUI.WindowPosition.X + InventoryUI.WindowSize.X + padding, InventoryUI.WindowPosition.Y);

        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
        ImGui.Begin(Title, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration |
                           ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), Title);

        var armorOrigin = new Vector2(padding, padding + titleHeight);
        DrawSlots(equipment, _armorSlots, armorOrigin, slotStep);

        var upgradesOrigin = new Vector2(padding, armorOrigin.Y + armorHeight + upgradesGap);
        DrawSlots(equipment, _upgradeSlots, upgradesOrigin, slotStep);

        ImGui.End();
    }

    private static void DrawSlots(PlayerEquipment equipment, EquipmentSlotType[] types, Vector2 origin, float slotStep)
    {
        for (int i = 0; i < types.Length; i++)
        {
            var slot = equipment.GetSlot(types[i]);
            ImGui.SetCursorPos(origin + new Vector2(i % Columns * slotStep, i / Columns * slotStep));
            InventoryUIHelper.DrawSlot(slot, slot.Name, _onSlotClicked, UIAtlas.Get(_backgroundNames[(int)types[i]]));
            InventoryUIHelper.ShowTooltip(slot, false, true);
        }
    }

    private static void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }
}
