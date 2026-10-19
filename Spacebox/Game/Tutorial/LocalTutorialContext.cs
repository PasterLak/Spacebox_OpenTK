using Engine;
using Engine.InputPro;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Tutorial;

public sealed class LocalTutorialContext : ITutorialContext, ITutorialTextResolver
{
    private readonly LocalAstronaut _player;
    private readonly string _modId;

    public LocalTutorialContext(LocalAstronaut player, string modId)
    {
        _player = player;
        _modId = modId;
    }

    public GameMode GameMode => _player.GameMode;
    public Vector3 ViewDirection => _player.Front;
    public short SelectedItemId => PanelUI.CurrentSlot() is { HasItem: true } slot ? slot.Item.Id : (short)0;
    public bool IsMagnetEnabled => BlockSelector.Instance != null && BlockSelector.Instance.EnableMagnet;
    public bool IsFlashlightOn => _player.Flashlight != null && _player.Flashlight.Enabled;
    public bool IsPiloting => ShipPilot.IsPiloting;
    public bool IsPilotingFlight => ShipPilot.Console?.Data.Flight == true;
    public bool IsUsingShipCamera => ShipPilot.IsUsingCamera;
    public bool IsInGravity => _player.Gravity.IsActive;

    public bool HasAnyEquipment
    {
        get
        {
            var storage = _player.Equipment.Storage;
            for (byte x = 0; x < storage.SizeX; x++)
            {
                if (storage.GetSlot(x, 0).HasItem) return true;
            }
            return false;
        }
    }

    public int CountItem(short itemId)
    {
        if (!GameAssets.TryGetItemById(itemId, out var item)) return 0;
        return _player.Panel.GetTotalCountOf(item) + _player.Inventory.GetTotalCountOf(item);
    }

    public bool IsEquipped(short itemId)
    {
        var storage = _player.Equipment.Storage;
        for (byte x = 0; x < storage.SizeX; x++)
        {
            var slot = storage.GetSlot(x, 0);
            if (slot.HasItem && slot.Item.Id == itemId) return true;
        }
        return false;
    }

    public bool IsSlotEquipped(EquipmentSlotType slot)
    {
        return _player.Equipment.GetSlot(slot).HasItem;
    }

    public bool IsWindowOpen(string window)
    {
        return !string.IsNullOrEmpty(window) && UIManager.IsOpen(window);
    }

    public bool IsActionPressed(string action)
    {
        return InputManager.Instance.HasAction(action) && Input.IsActionDown(action);
    }

    public bool IsActionHeld(string action)
    {
        return InputManager.Instance.HasAction(action) && Input.IsAction(action);
    }

    public double GetStat(string stat)
    {
        return PlayerStatReader.Read(_player.PlayerStatistics, stat);
    }

    public string KeyName(string action)
    {
        return KeyNames.ForAction(action);
    }

    public string ItemName(string itemKey)
    {
        if (string.IsNullOrWhiteSpace(itemKey)) return null;

        string fullId = itemKey.Contains(':') ? itemKey : Resource.GameSetLoader.CombineId(_modId, itemKey);
        return GameAssets.TryGetItemByFullID(fullId, out var item) && item != null ? item.Name : null;
    }
}
