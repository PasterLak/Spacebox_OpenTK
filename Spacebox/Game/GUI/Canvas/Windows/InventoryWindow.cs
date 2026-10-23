using Engine;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class InventoryWindow : GameWindow
{
    private readonly StorageGridControl _grid = new StorageGridControl();

    private readonly InventoryUI _inventory;

    public InventoryWindow(InventoryUI inventory) : base("Inventory")
    {
        _inventory = inventory;
        Name = "Inventory";
        AddHeaderButton(StorageButtons.CombineText, StorageButtons.CombineTooltip, () => StorageButtons.Combine(Storage));
        AddHeaderButton(StorageButtons.SortText, StorageButtons.SortTooltip, () => StorageButtons.Sort(Storage));
        _grid.SlotClicked += _inventory.OnSlotClicked;
        Body.AddChild(_grid);
    }

    private Storage Storage => _inventory.Player?.Inventory;

    public override Sounds.UiSoundSpec OpenSound => default;

    public override Sounds.UiSoundSpec CloseSound => default;

    public override bool IsOpen => UIManager.IsOpen(HudVisibility.ItemWindow) && Storage != null;

    public override int SoundPriority => 0;

    protected override void Refresh() => _grid.Show(Storage);
}
