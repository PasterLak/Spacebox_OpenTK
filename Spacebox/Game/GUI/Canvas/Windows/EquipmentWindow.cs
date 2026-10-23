using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Items;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class EquipmentWindow : GameWindow
{
    private const float SlotGap = 4f;
    private const float PreviewColumns = 2.8f;

    private readonly List<(ItemSlotControl Control, EquipmentSlotType Type)> _slots = new List<(ItemSlotControl, EquipmentSlotType)>();
    private readonly PlayerPreviewControl _preview;
    private PlayerEquipment _equipment;

    public EquipmentWindow() : base("Equipment")
    {
        Name = "Equipment";
        float slot = ItemSlotControl.DefaultSize;
        float height = EquipmentUI.Rows * slot + (EquipmentUI.Rows - 1) * SlotGap;

        var row = Body.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 6f);
        row.AddChild(Column(0));
        _preview = row.AddChild(new PlayerPreviewControl(() => EquipmentUI.Preview, source => _equipment?.TryEquip(source)) { CustomMinimumSize = new Vector2(MathF.Round((slot + SlotGap) * PreviewColumns), height) });
        row.AddChild(Column(1));
        row.AddChild(new VSeparator());

        var devices = row.AddChild(new VBoxContainer());
        devices.AddThemeConstantOverride("separation", SlotGap);
        foreach (var type in EquipmentUI.DeviceSlots) devices.AddChild(CreateSlot(type));
    }

    public override bool IsOpen => UIManager.IsOpen(HudVisibility.ItemWindow) && InventoryUI.Player?.Equipment != null;

    protected override void Refresh()
    {
        var player = InventoryUI.Player;
        var equipment = player.Equipment;
        if (!ReferenceEquals(equipment, _equipment))
        {
            _equipment = equipment;
            foreach (var (control, type) in _slots) control.Bind(equipment.GetSlot(type), UIAtlas.Get(EquipmentUI.BackgroundName(type)));
        }

        foreach (var (control, _) in _slots) control.Refresh();
        EquipmentUI.Preview.Prepare(player, equipment, PanelUI.HeldItem, _preview.PixelSize);
    }

    private VBoxContainer Column(int column)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", SlotGap);
        for (int row = 0; row < EquipmentUI.Rows; row++)
        {
            var placement = Array.Find(EquipmentUI.ArmorLayout, p => p.Column == column && p.Row == row);
            bool exists = Array.Exists(EquipmentUI.ArmorLayout, p => p.Column == column && p.Row == row);
            box.AddChild(exists ? CreateSlot(placement.Type) : Spacer(ItemSlotControl.DefaultSize, ItemSlotControl.DefaultSize));
        }
        return box;
    }

    private ItemSlotControl CreateSlot(EquipmentSlotType type)
    {
        var control = new ItemSlotControl();
        control.Clicked += EquipmentUI.OnSlotClicked;
        _slots.Add((control, type));
        return control;
    }
}
