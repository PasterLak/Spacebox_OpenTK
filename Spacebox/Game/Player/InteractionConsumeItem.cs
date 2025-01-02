using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Player;

public class InteractionConsumeItem : InteractionMode
{

    private ItemSlot _itemSlot;
    private const byte MaxDestroyDistance = 6;

    public InteractionConsumeItem(ItemSlot itemSlot)
    {
        _itemSlot = itemSlot;
    }

    public override void OnEnable()
    {
        
    }

    public override void OnDisable()
    {
        
    }

    public override void Update(Astronaut player)
    {
        if(!Input.IsMouseButtonDown(MouseButton.Right)) return;

        if (_itemSlot == null) return;
        if (_itemSlot.Item == null) return;
        if(!_itemSlot.HasItem) return;

        var consumable = _itemSlot.Item as ConsumableItem;
        
        if(consumable == null) return;
       
        
        ApplyConsumable(consumable, player);
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
            if (GameBlocks.TryGetItemSound(consumable.Id, out AudioClip clip))
            {
                /*if (useConsumableAudio != null)
                {
                    useConsumableAudio.Stop();
                }

                useConsumableAudio = new AudioSource(clip);
                useConsumableAudio.Volume = 0.3f;
                useConsumableAudio.Play();*/

                if (consumable.HealAmount > 0)
                    HealthColorOverlay.SetActive(new System.Numerics.Vector3(0, 1, 0), 0.2f);

                if (consumable.PowerAmount > 0)
                    HealthColorOverlay.SetActive(new System.Numerics.Vector3(0, 0, 1), 0.15f);
            }

            player.HealthBar.StatsData.Increment(consumable.HealAmount);
            player.PowerBar.StatsData.Increment(consumable.PowerAmount);
        }
        else
        {
            Debug.Error($"[Astrounaut] ApplyConsumable: consumable was null!");
        }
    }
}