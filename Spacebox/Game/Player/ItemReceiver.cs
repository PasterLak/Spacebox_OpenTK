using Engine.Audio;

namespace Spacebox.Game.Player;

public static class ItemReceiver
{
    private static AudioSource _sound;

    public static bool CanReceive(Item item)
    {
        return item != null && item.Id > 0;
    }

    public static bool HasUseEffect(ConsumableItem consumable)
    {
        return consumable.HealAmount > 0 || consumable.PowerAmount > 0 || consumable.OxygenAmount > 0;
    }

    public static bool TryReceive(LocalAstronaut player, Item item, out string failure)
    {
        failure = null;
        if (player == null || !CanReceive(item))
        {
            failure = "This item cannot be given";
            return false;
        }

        if (!player.IsAlive)
        {
            failure = "The player is dead";
            return false;
        }

        if (item is EquipmentItem equipment && player.Equipment.TryEquipIfEmpty(equipment))
        {
            PlaySound(equipment);
            player.NotifyItemUsed(equipment);
            return true;
        }

        if (item is ConsumableItem consumable && HasUseEffect(consumable))
        {
            PlaySound(consumable);
            ConsumableUse.Apply(player, consumable);
            return true;
        }

        if (player.Panel.TryAddItem(item, 1) || player.Inventory.TryAddItem(item, 1)) return true;

        failure = "Inventory is full";
        return false;
    }

    private static void PlaySound(Item item)
    {
        var clip = ItemUseCue.GetSound(item);
        if (clip == null) return;

        if (_sound == null || _sound.IsDisposed)
            _sound = new AudioSource(clip) { Volume = 0.4f };

        if (_sound.IsPlaying) _sound.Stop();
        _sound.Clip = clip;
        _sound.Play();
    }
}
