using Engine;
using Engine.Audio;
using Engine.Graphics.Atlas;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Disassembly;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI;

public sealed class DisassemblerUI : IGameUiController
{
    public const string WindowId = "disassembler";

    private readonly Action<DisassemblerBlock> _onDisassembled;

    private DisassemblerBlock _block;
    private LocalAstronaut _player;
    private string _blockName = "Disassembler";
    private bool _wasVisible;
    private AudioSource _craftedSound;

    public DisassemblerUI()
    {
        _onDisassembled = OnDisassembled;
    }

    public void Open(DisassemblerBlock block, LocalAstronaut player)
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

    public bool IsOpen => UIManager.IsOpen(WindowId) && _block != null;
    public DisassemblerBlock Block => _block;
    public string BlockName => _blockName;
    public AtlasSprite InputIcon => UIAtlas.Get("disassemblerinput");
    public AtlasSprite OutputIcon => UIAtlas.Get("furnaceoutput");

    public void OnGUI()
    {
        bool isVisible = IsOpen;
        if (_wasVisible && !isVisible) OnClosed();
        _wasVisible = isVisible;
        if (isVisible && Input.IsKeyDown(Keys.Escape)) UIManager.CloseAll();
    }

    public float Progress => _block != null && _block.HasPower ? _block.ProgressNormalized : 0f;

    public (string Text, Spacebox.Game.GUI.Canvas.Windows.StatusKind Kind) Status()
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

    public DisassemblyRecipe PreviewRecipe()
    {
        if (_block == null) return null;
        var item = GetPreviewItem();
        return item != null && DisassemblyRecipes.TryGet(item, out var recipe) ? recipe : null;
    }

    private Item GetPreviewItem()
    {
        if (_block.CurrentItemId != 0 && GameAssets.TryGetItemById(_block.CurrentItemId, out var current)) return current;

        foreach (var slot in _block.InputStorage.GetAllSlots())
        {
            if (slot.HasItem) return slot.Item;
        }

        return null;
    }

    public void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }

    private void OnDisassembled(DisassemblerBlock block)
    {
        if (block != _block || !UIManager.IsOpen(WindowId) || _craftedSound == null) return;
        if (HasFinishedSound(block)) return;

        if (_craftedSound.IsPlaying) _craftedSound.Stop();
        _craftedSound.Play();
    }

    private bool HasFinishedSound(DisassemblerBlock block) => !string.IsNullOrEmpty(GameAssets.GetBlockDataById(block.Id).FinishedSound);

    public void Reset()
    {
        if (_block != null) _block.OnDisassembled -= _onDisassembled;
        _craftedSound?.Dispose();
        _craftedSound = null;
        _block = null;
        _player = null;
        _wasVisible = false;
    }

    private void OnClosed()
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
