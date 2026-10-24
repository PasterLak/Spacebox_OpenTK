using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Network;

namespace Spacebox.Game.Pictures;

public sealed class PictureFrameInteraction
{
    private bool _pending;

    public void Use(PictureFrameBlock frame, LocalAstronaut player)
    {
        if (_pending || frame == null || player == null) return;

        var slot = PanelUI.CurrentSlot();
        var held = slot?.Item as PictureItem;
        if (slot == null || !slot.HasItem) held = null;
        if (!frame.CanSwap(held)) return;

        _pending = true;
        PictureFrameNetwork.RequestSwap(frame, held, (granted, removed) =>
        {
            _pending = false;
            if (!granted) return;

            if (held != null && PlacementRefund.ConsumesItems(player.GameMode)) TakeFromHand(player, slot, held);
            if (removed != null) PlayerItemGiver.Give(player, removed, 1);
        });
    }


    private static void TakeFromHand(LocalAstronaut player, ItemSlot slot, PictureItem picture)
    {
        if (slot.HasItem && slot.Item == picture)
        {
            slot.TakeOne();
            return;
        }

        if (!player.Panel.TryRemoveItem(picture, 1)) player.Inventory.TryRemoveItem(picture, 1);
    }
}
