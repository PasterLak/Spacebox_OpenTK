using Engine;
using Engine.Audio;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Effects;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Player.Interactions;

public class InteractionConsumeItem : InteractionMode
{

    private ItemSlot _itemSlot;
    private static AudioSource useConsumableAudio;
    public InteractionConsumeItem(ItemSlot itemSlot)
    {
        _itemSlot = itemSlot;
        AllowReload = true;

    }

    public override void OnEnable()
    {

    }

    public override void OnDisable()
    {

    }

    public override void Update(Astronaut player)
    {
        if (!player.CanMove) return;
        if (!Input.IsActionDown("use")) return;

        if (_itemSlot == null) return;
        if (_itemSlot.Item == null) return;
        if (!_itemSlot.HasItem) return;

        var consumable = _itemSlot.Item as ConsumableItem;

        if (consumable == null) return;

        ApplyConsumable(consumable, player);

        if (GameMode == GameModes.GameMode.Survival)
            _itemSlot.DropOne();

        if (_itemSlot.Count == 0)
        {
            _itemSlot = null;
            player.SetInteraction(new InteractionDefault());
        }

    }

    private void ApplyConsumable(ConsumableItem consumable, Astronaut player)
    {
        if (consumable != null)
        {
            if (GameAssets.TryGetItemSound(consumable.Id, out AudioClip clip))
            {
                if (useConsumableAudio != null)
                {
                    useConsumableAudio.Stop();
                }

                useConsumableAudio = new AudioSource(clip);
                useConsumableAudio.Volume = 0.3f;
                useConsumableAudio.Play();

                if (consumable.HealAmount > 0)
                {
                    ColorOverlay.FadeOut(new System.Numerics.Vector3(0, 1, 0), 0.2f);
                    player.Effects.PlayEffect(PlayerEffectType.Heal);
                }
                    

                if (consumable.PowerAmount > 0)
                {
                    ColorOverlay.FadeOut(new System.Numerics.Vector3(0, 0, 1), 0.15f);
                    player.Effects.PlayEffect(PlayerEffectType.Charge);
                }
                 
            }
            else
            {
                Debug.Error($"[InteractionConsumeItem] sound effect was not played because was not found!");
            }

            player.HealthBar.StatsData.Increment(consumable.HealAmount);
            player.PowerBar.StatsData.Increment(consumable.PowerAmount);
        }
        else
        {
            Debug.Error($"[InteractionConsumeItem] ApplyConsumable: consumable was null!");
        }
    }

    public override void Render(Astronaut player)
    {

    }
}