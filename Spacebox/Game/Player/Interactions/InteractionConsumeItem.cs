using Engine;
using Engine.Audio;
using Engine.Physics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player.Interactions;

public class InteractionConsumeItem : InteractionMode
{

    private ItemSlot _itemSlot;
    private AudioSource useConsumableAudio;

    private float ticks = 0;
    private float cooldown = 0f;

    public InteractionConsumeItem(ItemSlot itemSlot)
    {
        _itemSlot = itemSlot;
        AllowReload = true;

    }

    public override void OnEnable()
    {

        var consumable = _itemSlot.Item as ConsumableItem;

        if (consumable == null)
        {
            Debug.Error($"[InteractionConsumeItem] OnEnable: item in slot is not a consumable!");
            return;
        }
        else
        {
            cooldown = consumable.UseCooldown;
            ticks = cooldown;
        }


        if (GameAssets.TryGetItemSound(consumable.Id, out AudioClip clip))
        {
            useConsumableAudio = new AudioSource(clip);
            useConsumableAudio.Volume = 0.3f;
        }
        else
        {
            Debug.Error($"[InteractionConsumeItem] OnEnable: sound for consumable item id {consumable.Id} not found!");
        }

    }

    public override void OnDisable()
    {
        if (useConsumableAudio != null)
        {
            useConsumableAudio.Dispose();
            useConsumableAudio = null;
        }
    }

    public override void Update(LocalAstronaut player)
    {
        if (!player.CanMove) return;

        if (ticks < cooldown)
        {
            ticks += Time.Delta;
        }
        else
        {
            ticks = cooldown;
        }

        if (BlockUseTarget.Update(player)) return;

        if (ticks < cooldown) return;
        if (!Input.IsActionDown("use")) return;

        if (_itemSlot == null) return;
        if (_itemSlot.Item == null) return;
        if (!_itemSlot.HasItem) return;

        var consumable = _itemSlot.Item as ConsumableItem;

        if (consumable == null) return;



        ApplyConsumable(consumable, player);
        ticks = 0;

        if (GameMode == GameModes.GameMode.Survival)
            _itemSlot.DropOne();

        if (_itemSlot.Count == 0)
        {
            _itemSlot = null;
            player.SetInteraction(new InteractionDefault());
        }

    }

    private void ApplyConsumable(ConsumableItem consumable, LocalAstronaut player)
    {
        if (consumable != null)
        {
            if (useConsumableAudio != null)
            {

                useConsumableAudio.Stop();

                useConsumableAudio.Play();
            }
            player.PlayerStatistics.ItemsConsumed++;
            player.PlayerStatistics.RecordItemUse(consumable.Id_string);

            ConsumableUse.Apply(player, consumable);
        }
        else
        {
            Debug.Error($"[InteractionConsumeItem] ApplyConsumable: consumable was null!");
        }
    }

    public override void Render(LocalAstronaut player)
    {

    }
}