using Spacebox.Game.Generation;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Player;

public static class PlacementRefund
{
    public static bool ConsumesItems(GameMode mode) => mode == GameMode.Survival;

    public static void Refund(LocalAstronaut player, short blockId)
    {
        if (player == null || !ConsumesItems(player.GameMode)) return;
        if (!GameAssets.TryGetItemByBlockID(blockId, out var item)) return;

        if (player.Panel.TryAddItem(item, 1) || player.Inventory.TryAddItem(item, 1)) return;

        World.DropEffectManager?.DropItem(player.Position, player.Front, 1f, item, 1);
    }
}
