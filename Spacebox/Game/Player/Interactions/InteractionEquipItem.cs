using Engine;
using Engine.Audio;
using Engine.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Interactions;

public sealed class InteractionEquipItem : InteractionDefault
{
    private readonly ItemSlot _slot;

    public InteractionEquipItem(ItemSlot slot)
    {
        _slot = slot;
    }

    public override void Update(LocalAstronaut player)
    {
        if (IsLookingAtInteractiveBlock(player))
        {
            base.Update(player);
            return;
        }

        if (!player.CanMove || !Input.IsActionDown("use")) return;

        var item = _slot.Item;
        if (!player.Equipment.TryEquip(_slot)) return;

        player.NotifyItemUsed(item);
    }

    private static bool IsLookingAtInteractiveBlock(LocalAstronaut player)
    {
        var sector = World.CurrentSector;
        if (sector == null) return false;

        var ray = new Ray(player.Position, player.Front, InteractiveBlock.InteractionDistance);
        return sector.Raycast(ray, out var hit) && hit.block != null && hit.block.Is<InteractiveBlock>(out _);
    }
}
