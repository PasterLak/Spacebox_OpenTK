using Engine.Graphics.Atlas;
using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI;

public static class OxygenGeneratorUI
{
    private const string WindowId = "oxygenGenerator";
    public const int TankSteps = 3;

    private static OxygenGeneratorBlock _block;
    private static LocalAstronaut _player;
    private static string _blockName = "Oxygen Generator";
    private static bool _wasVisible;

    public static void Open(OxygenGeneratorBlock block, LocalAstronaut player)
    {
        _block = block;
        _player = player;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;

        block.WaterStorage.ConnectStorage(player.Inventory);
        player.Inventory.ConnectStorage(block.WaterStorage);
        player.Panel.ConnectStorage(block.WaterStorage);

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

    public static OxygenGeneratorBlock Block => _block;
    public static ItemSlot WaterSlot => _block?.WaterStorage.GetSlot(0, 0);
    public static AtlasSprite WaterIcon => UIAtlas.Get("water");

    public static (string Text, Spacebox.Game.GUI.Canvas.Windows.StatusKind Kind, string Detail) Status()
    {
        if (_block == null) return ("", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Muted, "");
        if (!_block.IsActive) return ("NO POWER", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Bad, "");
        if (!_block.HasWater) return ("No water", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Warning, "");
        if (_block.IsOutside) return ("Room not sealed", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Warning, "Air leaks out");

        float secondsLeft = (1f - _block.ConsumeProgress) * _block.WaterConsumeInterval;
        return ("Working", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Good, $"Next water use: {secondsLeft:0}s");
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

        _block?.WaterStorage.DisconnectStorage();
        _block = null;
    }
}
