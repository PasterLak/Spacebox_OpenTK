using Engine;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class InventoryWindow : GameWindow
{
    private readonly StorageGridControl _grid = new StorageGridControl();

    public InventoryWindow() : base("Inventory")
    {
        Name = "Inventory";
        AddHeaderButton(StorageButtons.CombineText, StorageButtons.CombineTooltip, () => StorageButtons.Combine(Storage));
        AddHeaderButton(StorageButtons.SortText, StorageButtons.SortTooltip, () => StorageButtons.Sort(Storage));
        _grid.SlotClicked += InventoryUI.OnSlotClicked;
        Body.AddChild(_grid);
    }

    private static Storage Storage => InventoryUI.Player?.Inventory;

    public override bool IsOpen => UIManager.IsOpen(HudVisibility.ItemWindow) && Storage != null;

    protected override void Refresh() => _grid.Show(Storage);
}
