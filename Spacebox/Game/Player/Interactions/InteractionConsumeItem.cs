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
    private InteractiveBlock lastInteractiveBlock;

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
            useConsumableAudio.Stop();
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

        Ray rayNormal = new Ray(player.Position, player.Front, InteractiveBlock.InteractionDistance);
        HitInfo hit;

        if (World.CurrentSector.Raycast(rayNormal, out hit))
        {
            if (hit.block.Is<InteractiveBlock>(out var b))
            {
                lastInteractiveBlock = b;
                InteractiveBlock.UpdateInteractive(lastInteractiveBlock, player, ref hit);

                if (hit.block.Is<StorageBlock>(out var storageBlock))
                {
                    storageBlock.SetPositionInChunk(hit.blockPositionIndex);
                }
            }

            return;
        }

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
            player.PlayerStatistics.ItemsСonsumed++;

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


            player.HealthBar.StatsData.Increment(consumable.HealAmount);
            player.PowerBar.StatsData.Increment(consumable.PowerAmount);
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