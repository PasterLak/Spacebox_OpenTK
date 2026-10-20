using Spacebox.Game.Generation;

namespace Spacebox.Game.Player;

public static class PlayerItemGiver
{
    public static int StackSize(Item item) => Math.Max(1, (int)item.StackSize);

    public static void Give(LocalAstronaut player, Item item, int count)
    {
        if (player == null || item == null) return;

        int left = count;
        while (left > 0)
        {
            byte stack = (byte)Math.Min(left, Math.Min(StackSize(item), byte.MaxValue));
            if (!player.Panel.TryAddItem(item, stack, out var notAdded) && !player.Inventory.TryAddItem(item, notAdded, out notAdded))
                World.DropEffectManager?.DropItem(player.Position, player.Front, 1f, item, notAdded);
            left -= stack;
        }
    }
}
