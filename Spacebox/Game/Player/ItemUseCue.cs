using Engine.Audio;
using Spacebox.Game.Effects;

namespace Spacebox.Game.Player;

public static class ItemUseCue
{
    public const string EquipSound = "Resources/Audio/flashlight.ogg";

    public static bool IsReplicated(Item item)
    {
        return item is ConsumableItem or EquipmentItem;
    }

    public static AudioClip GetSound(Item item)
    {
        if (item is ConsumableItem && GameAssets.TryGetItemSound(item.Id, out AudioClip clip)) return clip;
        if (item is EquipmentItem equipment) return GameAssets.Sounds.TryGetValue(equipment.EquipSound, out var equipClip) && equipClip != null ? equipClip : GameAssets.LoadResource<AudioClip>(EquipSound);
        return null;
    }

    public static void PlayEffects(PlayerEffects effects, Item item)
    {
        if (effects == null || item is not ConsumableItem consumable) return;

        if (consumable.HealAmount > 0) effects.PlayEffect(PlayerEffectType.Heal);
        if (consumable.PowerAmount > 0) effects.PlayEffect(PlayerEffectType.Charge);
        if (consumable.OxygenAmount > 0) effects.PlayEffect(PlayerEffectType.Oxygen);
    }
}
