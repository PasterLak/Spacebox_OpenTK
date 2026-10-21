using System.Numerics;
using Engine;
using ImGuiNET;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class EquipmentUI
{
    private const string Title = "Equipment";
    private const string BackgroundsFolder = "equipment/";
    private const float SlotGapRatio = 0.08f;
    private const float WindowGapFactor = 3f;
    private const int Rows = 4;
    private const float PreviewColumns = 2.8f;

    private static readonly Vector4 TitleColor = new Vector4(0.9f, 0.9f, 0.9f, 1f);
    private static readonly Vector4 SeparatorColor = new Vector4(0.55f, 0.56f, 0.6f, 0.6f);

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

    private static readonly Action<ItemSlot> _onSlotClicked = OnSlotClicked;
    private static readonly string[] _backgroundNames = CreateBackgroundNames();
    private static PlayerPreview _preview = new PlayerPreview();

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

    public static void OnGUI(LocalAstronaut player)
    {
        var equipment = player?.Equipment;
        if (equipment == null || !UIManager.IsOpen("inventory")) return;

        var displaySize = ImGui.GetIO().DisplaySize;
        float slotSize = InventoryUIHelper.SlotSize;
        float padding = slotSize * 0.12f;
        float slotStep = slotSize * (1f + SlotGapRatio);
        float titleHeight = ImGui.GetTextLineHeight() + padding;
        float columnsHeight = Rows * slotStep - slotStep * SlotGapRatio;
        var previewSize = new Vector2(slotStep * PreviewColumns, columnsHeight);
        float gap = padding;
        float sectionGap = padding * 2.5f;

        float width = padding * 2f + slotSize * 3f + previewSize.X + gap * 2f + sectionGap;
        float height = padding * 2f + titleHeight + columnsHeight;
        var windowSize = new Vector2(width, height);
        var windowPos = PlaceWindow(windowSize, padding, displaySize);

        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
        ImGui.Begin(Title, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration |
                           ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, Title);

        float top = padding + titleHeight;
        float leftX = padding;
        float previewX = leftX + slotSize + gap;
        float rightX = previewX + previewSize.X + gap;
        float devicesX = rightX + slotSize + sectionGap;

        foreach (var placement in ArmorLayout)
        {
            float x = placement.Column == 0 ? leftX : rightX;
            DrawSlot(equipment, placement.Type, new Vector2(x, top + placement.Row * slotStep));
        }

        ImGui.SetCursorPos(new Vector2(previewX, top));
        bool shown = _preview.Show(player, equipment, PanelUI.HeldItem, previewSize);
        if (shown && InventoryUIHelper.AcceptSlotDrop(out var dropped)) equipment.TryEquip(dropped);
        if (shown && ImGui.IsItemHovered()) ImGui.SetTooltip("Drag to rotate, double-click to reset\nDrop equipment here to wear it");

        float separatorX = windowPos.X + rightX + slotSize + sectionGap * 0.5f;
        ImGui.GetWindowDrawList().AddLine(new Vector2(separatorX, windowPos.Y + top), new Vector2(separatorX, windowPos.Y + top + columnsHeight),
            ImGui.ColorConvertFloat4ToU32(SeparatorColor), 1f);

        for (int i = 0; i < DeviceSlots.Length; i++)
            DrawSlot(equipment, DeviceSlots[i], new Vector2(devicesX, top + i * slotStep));

        ImGui.End();
    }

    public static void RenderPreview()
    {
        _preview.Render();
    }

    public static void Reset()
    {
        _preview.Dispose();
        _preview = new PlayerPreview();
    }

    private static Vector2 PlaceWindow(Vector2 size, float gap, Vector2 displaySize)
    {
        float x = InventoryUI.WindowPosition.X + InventoryUI.WindowSize.X + gap * WindowGapFactor;
        float y = MathF.Max(gap, InventoryUI.WindowPosition.Y + InventoryUI.WindowSize.Y - size.Y);

        if (CraftingGUI.IsVisible)
        {
            float craftingBottom = CraftingGUI.WindowPosition.Y + CraftingGUI.WindowSize.Y + gap;
            float craftingRight = CraftingGUI.WindowPosition.X + CraftingGUI.WindowSize.X + gap;
            if (y < craftingBottom && x < craftingRight) x = craftingRight;
        }

        x = MathF.Min(x, MathF.Max(gap, displaySize.X - size.X - gap));
        return new Vector2(x, y);
    }

    private static void DrawSlot(PlayerEquipment equipment, EquipmentSlotType type, Vector2 position)
    {
        var slot = equipment.GetSlot(type);
        ImGui.SetCursorPos(position);
        InventoryUIHelper.DrawSlot(slot, slot.Name, _onSlotClicked, UIAtlas.Get(_backgroundNames[(int)type]));
        InventoryUIHelper.ShowTooltip(slot, false, true);
    }

    private static void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }
}
