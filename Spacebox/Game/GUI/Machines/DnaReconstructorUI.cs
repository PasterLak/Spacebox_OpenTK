using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI;

public static class DnaReconstructorUI
{
    private const string WindowId = "dnaReconstructor";
    private const string SlotIcon = "dna_ui";

    private static DnaReconstructorBlock _block;
    private static LocalAstronaut _player;
    private static string _blockName = "DNA Reconstructor";
    private static bool _wasVisible;

    public static void Open(DnaReconstructorBlock block, LocalAstronaut player)
    {
        _block = block;
        _player = player;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;

        block.DnaStorage.ConnectStorage(player.Inventory);
        player.Inventory.ConnectStorage(block.DnaStorage);
        player.Panel.ConnectStorage(block.DnaStorage);

        if (player.GameMode != GameMode.Survival)
            UIManager.Open(WindowId, "inventory", "creative");
        else
            UIManager.Open(WindowId, "inventory");
    }

    public static bool IsOpen => UIManager.IsOpen(WindowId) && _block != null;
    public static string BlockName => _blockName;

    public static void Update()
    {
        bool isVisible = IsOpen;
        if (_wasVisible && !isVisible) OnClosed();
        _wasVisible = isVisible;
        if (isVisible && Input.IsKeyDown(Keys.Escape)) UIManager.CloseAll();
    }

    public static DnaReconstructorBlock Block => _block;
    public static ItemSlot DnaSlot => _block?.DnaStorage.GetSlot(0, 0);
    public static Engine.Graphics.Atlas.AtlasSprite SlotSprite => UIAtlas.Get(SlotIcon);

    public static (string Text, Spacebox.Game.GUI.Canvas.Windows.StatusKind Kind) Status()
    {
        if (_block == null) return ("", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Muted);
        if (!_block.IsActive) return ("NO POWER", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Bad);
        if (!_block.HasDna) return ("No DNA sample", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Warning);
        return ("Ready", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Good);
    }

    public static void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }

    public static void Reset()
    {
        _block = null;
        _player = null;
        _wasVisible = false;
    }

    private static void OnClosed()
    {
        if (_player != null)
        {
            _player.Panel.ConnectStorage(_player.Inventory, true);
            _player.Inventory.ConnectStorage(_player.Panel);
        }

        _block?.DnaStorage.DisconnectStorage();
        _block = null;
    }
}
