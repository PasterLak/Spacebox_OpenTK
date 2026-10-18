using Engine;
using Engine.Audio;
using Engine.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Interactions;

public sealed class InteractionEquipItem : InteractionDefault
{
    private const string EquipSound = "Resources/Audio/flashlight.ogg";

    private readonly ItemSlot _slot;
    private AudioSource? _sound;

    public InteractionEquipItem(ItemSlot slot)
    {
        _slot = slot;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        _sound = new AudioSource(GameAssets.LoadResource<AudioClip>(EquipSound)) { Volume = 0.4f };
    }

    public override void OnDisable()
    {
        base.OnDisable();
        _sound?.Dispose();
        _sound = null;
    }

    public override void Update(LocalAstronaut player)
    {
        if (IsLookingAtInteractiveBlock(player))
        {
            base.Update(player);
            return;
        }

        if (!player.CanMove || !Input.IsActionDown("use")) return;
        if (!player.Equipment.TryEquip(_slot)) return;

        _sound?.Play();
    }

    private static bool IsLookingAtInteractiveBlock(LocalAstronaut player)
    {
        var sector = World.CurrentSector;
        if (sector == null) return false;

        var ray = new Ray(player.Position, player.Front, InteractiveBlock.InteractionDistance);
        return sector.Raycast(ray, out var hit) && hit.block != null && hit.block.Is<InteractiveBlock>(out _);
    }
}
