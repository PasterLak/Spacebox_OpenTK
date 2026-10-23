using Engine;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GUI;

public static class SlotTransfer
{
    public static event Action<ItemSlot, Item, int> ItemsDraggedOut;

    public static void Move(ItemSlot source, ItemSlot target)
    {
        if (source == null || target == null || ReferenceEquals(source, target) || !source.HasItem) return;

        var draggedItem = source.Item;
        int draggedCount = source.Count;
        source.SwapWith(target);
        int moved = Spacebox.Game.Player.PlayerStatistics.CountMovedFrom(draggedItem, draggedCount, source.Item, source.Count);
        if (moved > 0) ItemsDraggedOut?.Invoke(source, draggedItem, moved);
    }

    public static void DropInWorld(ItemSlot slot)
    {
        var camera = Camera.Main;
        if (slot == null || !slot.HasItem || camera == null || !Spacebox.Network.Containers.ContainerNetwork.CanDropFrom(slot)) return;

        World.DropEffectManager.DropItemSlot(camera.Position + camera.Front * 0.5f, camera.Front, 6f, slot, 2, 4);
        slot.Clear();
    }
}
