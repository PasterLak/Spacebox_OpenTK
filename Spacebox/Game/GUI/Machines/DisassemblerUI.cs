using Engine;
using Engine.Audio;
using Engine.Graphics.Atlas;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Disassembly;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI;

public static class DisassemblerUI
{
    public const string WindowId = "disassembler";

    private static readonly Action<DisassemblerBlock> _onDisassembled = OnDisassembled;

    private static DisassemblerBlock _block;
    private static LocalAstronaut _player;
    private static string _blockName = "Disassembler";
    private static bool _wasVisible;
    private static AudioSource _craftedSound;

    public static void Open(DisassemblerBlock block, LocalAstronaut player)
    {
        if (_block != null) _block.OnDisassembled -= _onDisassembled;

        _block = block;
        _player = player;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;
        block.ResetSyncedState();
        block.OnDisassembled += _onDisassembled;

        if (_craftedSound == null || _craftedSound.IsDisposed)
            _craftedSound = new AudioSource(Resources.Load<AudioClip>("crafted"), AudioChannel.UI);

        block.InputStorage.ConnectStorage(player.Inventory);
        block.OutputStorage.ConnectStorage(player.Inventory);
        player.Inventory.ConnectStorage(block.InputStorage);
        player.Panel.ConnectStorage(block.InputStorage);

        if (player.GameMode != GameMode.Survival)
            UIManager.Open(WindowId, "inventory", "creative");
        else
            UIManager.Open(WindowId, "inventory");
    }

    public static bool IsOpen => UIManager.IsOpen(WindowId) && _block != null;
    public static DisassemblerBlock Block => _block;
    public static string BlockName => _blockName;
    public static AtlasSprite InputIcon => UIAtlas.Get("disassemblerinput");
    public static AtlasSprite OutputIcon => UIAtlas.Get("furnaceoutput");

    public static void Update()
    {
        bool isVisible = IsOpen;
        if (_wasVisible && !isVisible) OnClosed();
        _wasVisible = isVisible;
        if (isVisible && Input.IsKeyDown(Keys.Escape)) UIManager.CloseAll();
    }

    public static float Progress => _block != null && _block.HasPower ? _block.ProgressNormalized : 0f;

    public static (string Text, Spacebox.Game.GUI.Canvas.Windows.StatusKind Kind) Status()
    {
        if (_block == null) return ("", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Muted);
        if (!_block.HasPower) return ("NO POWER", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Bad);

        return _block.Status switch
        {
            DisassemblerStatus.Working => ($"Working {(int)(_block.ProgressNormalized * 100f)}%", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Good),
            DisassemblerStatus.OutputFull => ("Output is full", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Warning),
            DisassemblerStatus.NotEnoughItems => ("Not enough items", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Warning),
            DisassemblerStatus.CannotDisassemble => ("Can't be disassembled", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Warning),
            _ => ("Put items to disassemble", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Normal)
        };
    }

    public static DisassemblyRecipe PreviewRecipe()
    {
        if (_block == null) return null;
        var item = GetPreviewItem();
        return item != null && DisassemblyRecipes.TryGet(item, out var recipe) ? recipe : null;
    }

    private static Item GetPreviewItem()
    {
        if (_block.CurrentItemId != 0 && GameAssets.TryGetItemById(_block.CurrentItemId, out var current)) return current;

        foreach (var slot in _block.InputStorage.GetAllSlots())
        {
            if (slot.HasItem) return slot.Item;
        }

        return null;
    }

    public static void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }

    private static void OnDisassembled(DisassemblerBlock block)
    {
        if (block != _block || !UIManager.IsOpen(WindowId) || _craftedSound == null) return;

        if (_craftedSound.IsPlaying) _craftedSound.Stop();
        _craftedSound.Play();
    }

    public static void Reset()
    {
        if (_block != null) _block.OnDisassembled -= _onDisassembled;
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

        if (_block != null)
        {
            _block.OnDisassembled -= _onDisassembled;
            _block.InputStorage.DisconnectStorage();
            _block.OutputStorage.DisconnectStorage();
        }

        _block = null;
    }
}
